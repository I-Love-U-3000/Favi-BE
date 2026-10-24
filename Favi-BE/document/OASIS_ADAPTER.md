# OASIS snapshot adapter cho Favi

Adapter offline nhỏ: đọc SQLite của OASIS, kiểm tra toàn bộ dữ liệu được hỗ trợ,
xuất graph CSV và SQL seed cho PostgreSQL của Favi. Không thêm service, package,
API endpoint, migration hoặc thay đổi feed. Python 3.10 trở lên, chỉ standard library.

## Luồng chạy

1. Chạy OASIS riêng với profile/tính cách của bạn. Cho phép create post, like,
   comment, follow; tắt repost/quote cho lần xuất này.
2. Pause hoặc đóng môi trường OASIS sau một bước hoàn chỉnh.
3. Chuẩn bị media map: mỗi `post_id` có URL ảnh thật và `is_nsfw` do bạn kiểm tra.
4. Chạy adapter và giữ snapshot bất biến.
5. Dùng CSV để nghiên cứu graph. Nếu cần demo, chạy SQL seed trên database Favi
   local đã migrate, rồi mở ứng dụng bằng tài khoản Favi hiện có.

Từ thư mục `Favi-BE/Favi-BE` (thư mục chứa solution):

```powershell
python tools/oasis_adapter/adapter.py --database C:/data/reddit_simulation.db --media-map C:/data/media-map.json --run-id community-001-t100 --output tools/oasis_adapter/output/t100
python -m unittest discover -s tools/oasis_adapter -p "test_*.py" -v
```

Mẫu media map: [media-map.example.json](../tools/oasis_adapter/media-map.example.json).
Thay ID và URL bằng dữ liệu thật của bạn. Adapter không tạo/tải ảnh và không tự
kiểm duyệt ảnh. Chấp nhận HTTP(S) hoặc đường dẫn `/seed-assets/` hiện có của gateway.
Kích thước ảnh chưa biết lưu `0`, format chưa biết lưu `unknown`.

Import SQL bằng `psql` (dùng cấu hình kết nối local của bạn; không ghi mật khẩu vào file):

```powershell
psql --dbname=favi --username=favi --host=localhost --set=ON_ERROR_STOP=1 --file=tools/oasis_adapter/output/t100/favi-seed.sql
```

SQL chứa một transaction, chạy tuần tự users → posts/media → follows → likes → comments.
Lỗi SQL/FK rollback toàn bộ khi chạy bằng lệnh trên. Không có DELETE/TRUNCATE.
Chỉ chạy trên database đồ án/local. Dữ liệu seed gốc vẫn tồn tại, nên nếu cần một
catalog thuần OASIS, tự chuẩn bị database nghiên cứu riêng trước khi import.

## Schema nguồn và mapping

Đối chiếu schema chính thức OASIS ngày 2026-10-25:
[user.sql](https://github.com/camel-ai/oasis/blob/main/oasis/social_platform/schema/user.sql),
[post.sql](https://github.com/camel-ai/oasis/blob/main/oasis/social_platform/schema/post.sql),
[like.sql](https://github.com/camel-ai/oasis/blob/main/oasis/social_platform/schema/like.sql),
[follow.sql](https://github.com/camel-ai/oasis/blob/main/oasis/social_platform/schema/follow.sql),
[comment.sql](https://github.com/camel-ai/oasis/blob/main/oasis/social_platform/schema/comment.sql).
Schema upstream có thể đổi; adapter kiểm tra các column bắt buộc và báo lỗi nếu thiếu.

| OASIS | Favi | Graph |
|---|---|---|
| `user.user_id` | `Profiles.Id` | User node |
| `post.post_id`, `user_id` | `Posts.Id`, `ProfileId` + một `PostMedias` | Post node |
| `like.user_id`, `post_id` | `Reactions`, Type = Like (0) | User–post positive edge |
| `follow.follower_id`, `followee_id` | `Follows` | Cạnh user–user có hướng |
| `comment` | `Comments`, không có parent | Chỉ nhập demo, chưa là positive edge |

ID dùng UUIDv5 từ `run-id + loại entity + ID nguồn`; cùng snapshot và run-id tạo
cùng ID trong graph và SQL. Dùng `user_id`, không dùng `agent_id`; hai giá trị này
không nhất thiết bằng nhau. Username là `oasis_` + UUID dạng hex; mọi profile có
role User, quyền Public, không bị ban. Không tạo tài khoản email/password/token;
agent là profile để hiển thị, chưa đăng nhập được. Dùng tài khoản Favi hiện có để demo.

`run-id` xác định **một snapshot bất biến**. SQL insert-only, conflict trên primary
key được bỏ qua để chạy lại cùng file không nhân đôi dữ liệu. Không đồng bộ các
thay đổi hay xóa sau import. Không dùng lại run-id cho nội dung thay đổi; adapter
không so sánh nội dung với PostgreSQL. Snapshot ở tick khác dùng run-id khác và
có ID khác: khi nghiên cứu nhiều mốc thời gian, nối identity qua `source_id`,
không JOIN trực tiếp UUID giữa các run-id.

## Validation gate và tính tái lập

- Mở SQLite read-only trong một read transaction, đọc cả dữ liệu WAL đã commit.
- Yêu cầu đủ năm bảng nguồn; bảng rỗng hợp lệ, ngoại trừ user phải có ít nhất một hàng.
- Kiểm tra tất cả ID, timestamp, tham chiếu, self-follow, duplicate like/follow,
  comment rỗng, repost/quote, URL ảnh và nhãn NSFW explicit.
- Timestamp không có timezone được hiểu là UTC. Không bịa timestamp cho hàng thiếu.
  Hành động phải sau khi user và post liên quan được tạo.
- Không sinh thêm cạnh hoặc ép phân phối 90-9-1: giữ nguyên phân phối OASIS.
  Muốn skew thực tế, cấu hình population/activity trong simulator trước khi chạy.
- Sort theo source ID; không chèn thời gian chạy hiện tại vào output.
- Chỉ publish thư mục output sau khi toàn bộ validate và export thành công;
  thư mục đã tồn tại bị từ chối, không ghi đè.
- Manifest lưu counts, SHA-256 snapshot, checksum từng file và các bảng không sử dụng.

## Output

| File | Nội dung |
|---|---|
| `snapshot.json` | Dữ liệu chuẩn hóa, ID nguồn và ID Favi |
| `favi-seed.sql` | Seed Profiles, Posts, PostMedias, Follows, Reactions, Comments |
| `users.csv` | Mapping user ID |
| `posts.csv` | Mapping post, tác giả, created_at, is_eligible |
| `user_post_edges.csv` | Like vào post an toàn, event_type và occurred_at |
| `user_user_edges.csv` | Follow có hướng và created_at |
| `manifest.json` | Phiên bản contract, định danh snapshot, số lượng và checksum |

Post NSFW vẫn có trong snapshot/SQL với cờ `IsNSFW=true`, nhưng bị loại khỏi
`user_post_edges.csv`; lọc `posts.csv` theo `is_eligible=1` trước khi build graph.
Adapter không sửa các filter an toàn trong feed hiện tại. Không nhập dislike,
mute, save, repost, trace, impression hay event lịch sử. Không phát notification,
gửi email hoặc gọi dịch vụ vector/NSFW; đây là offline fixture import, không phải
write path nghiệp vụ hay consumer nên không thêm Outbox/Inbox hoặc HTTP idempotency.
Code chỉ nằm ở tool seed, không tạo dependency giữa các module và không sửa gateway.

Snapshot biểu diễn **trạng thái đang còn tồn tại**, không khôi phục unlike/unfollow
đã mất. Muốn temporal evaluation đầy đủ cần giữ event log của simulator riêng;
không suy ra lịch sử hoặc snapshot quá khứ chỉ từ database cuối cùng.

## Giới hạn kiểm thử

Unit tests dùng fixture SQLite theo tên column upstream; kiểm tra mapping, graph,
tái lập, validation và chạy SQL hai lần trên relational SQLite stand-in. Đây không
phải bằng chứng đã chạy OASIS/LLM thật hoặc import PostgreSQL thật. Cần smoke trên
database local đã migrate trước khi dùng cho demo. Không cần build .NET vì không
thay code ứng dụng. Rollback code bằng bỏ các file tool/tài liệu; dữ liệu đã import
phải xử lý trên database nghiên cứu riêng, adapter không cung cấp lệnh xóa.
