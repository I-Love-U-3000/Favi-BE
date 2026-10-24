# Handover OASIS adapter

Ngày: 2026-10-25. Branch: `feat/oasis-adapter`.
Người dùng đã yêu cầu commit/push branch này ở lượt tiếp theo; thay đổi được đóng
gói trong commit `feat(seed): add offline OASIS snapshot adapter`.

## Đã hoàn thành

- Tạo và checkout branch riêng từ `main`.
- Viết `tools/oasis_adapter/adapter.py`, Python standard library, không cần cài OASIS.
- Đọc database SQLite của OASIS theo schema chính thức: user, post, like, follow, comment.
- Kiểm tra schema, ID, timestamp, referential integrity, duplicate edge, self-follow,
  media map, nhãn NSFW và trường hợp không hỗ trợ trước khi xuất bất kỳ snapshot nào.
- Xuất snapshot JSON, graph CSV user–post/user–user, mapping GUID ổn định và manifest checksum.
- Sinh `favi-seed.sql` để import offline vào schema PostgreSQL hiện tại của Favi.
  Import tuần tự trong một transaction, insert-only, chạy lại cùng file không nhân đôi.
- Thêm media map mẫu, tài liệu kỹ thuật và link từ `document/README.md`.
- Thêm 12 unit tests; đã sửa lỗi giữ kết nối SQLite khiến Windows khóa file nguồn.

## Các file để review

| File | Vai trò |
|---|---|
| [adapter.py](tools/oasis_adapter/adapter.py) | CLI đọc SQLite, validate, xuất graph và SQL |
| [test_adapter.py](tools/oasis_adapter/test_adapter.py) | Fixture và 12 tests |
| [media-map.example.json](tools/oasis_adapter/media-map.example.json) | Mẫu ánh xạ post OASIS sang ảnh |
| [OASIS_ADAPTER.md](document/OASIS_ADAPTER.md) | Contract, mapping, cách chạy, giới hạn |
| [README.md](document/README.md) | Link đến tài liệu adapter |
| [tools/oasis_adapter/.gitignore](tools/oasis_adapter/.gitignore) | Bỏ qua output, SQLite và Python cache |
| [.tools/.gitignore](.tools/.gitignore) | Bỏ qua runtime Python/cache tải local để test |

## Cách chạy tiếp

Từ thư mục chứa solution `Favi-BE/Favi-BE`:

```powershell
python tools/oasis_adapter/adapter.py --database C:/data/reddit_simulation.db --media-map C:/data/media-map.json --run-id community-001-t100 --output tools/oasis_adapter/output/t100
psql --dbname=favi --username=favi --host=localhost --set=ON_ERROR_STOP=1 --file=tools/oasis_adapter/output/t100/favi-seed.sql
```

Cần pause/đóng OASIS, chuẩn bị URL ảnh thật cho mỗi post và database Favi local đã
migrate trước khi chạy. `run-id` xác định snapshot bất biến; không tái sử dụng nó
cho dữ liệu đã thay đổi. UUID giữa các run-id khác nhau khác nhau; so sánh nhiều
snapshot bằng source ID. Output đã tồn tại sẽ bị từ chối, không bị ghi đè.

## Kiểm tra đã chạy

Máy không có Python hoạt động, nên đã tải Python 3.11.14 vào `.tools/python/`
với quyền được chấp thuận. Runtime và cache được ignore, không cần commit.

Lệnh thực tế đã chạy thành công:

```powershell
& '.tools/python/cpython-3.11.14-windows-x86_64-none/python.exe' -m unittest discover -s tools/oasis_adapter -p 'test_*.py' -v
& '.tools/python/cpython-3.11.14-windows-x86_64-none/python.exe' tools/oasis_adapter/adapter.py --help
git diff --check
```

Kết quả: **12/12 tests pass**, CLI help hoạt động, diff không lỗi whitespace.
Tests bao gồm xuất lặp lại deterministic, namespace ID, SQL replay, Unicode/SQL
literal escaping, FK, orphan/timestamp invalid, duplicate/self-follow, repost,
media/NUL invalid, đọc committed WAL, schema/source thiếu và từ chối ghi đè.
SQL được smoke trên SQLite stand-in có ràng buộc quan hệ, không phải PostgreSQL thật.

## Giới hạn và việc chưa làm

- Chưa chạy OASIS/LLM thật; chưa có database mô phỏng của người dùng để kiểm tra.
- Chưa import PostgreSQL thật: Docker daemon không chạy và máy không có `psql` sẵn.
- Chưa chỉnh .NET, frontend, feed, DI, gateway hay database schema.
- Không có live adapter cho agent gọi API Favi. Đây là snapshot/export/import adapter.
- Không tự tạo/tải ảnh, không tự phân loại NSFW. Media map phải do người dùng chuẩn bị.
- Không tạo email/password/token cho agent. Agent import là profile public với role User;
  dùng tài khoản Favi hiện có để xem cộng đồng.
- Không nhập save, dislike, mute, repost/quote, impression, agent memory hoặc trace.
  Bảng không dùng được liệt kê trong manifest; repost/quote trong post bị từ chối.
- Graph positive chỉ là like vào bài được đánh dấu an toàn. SQL vẫn giữ post NSFW
  với cờ tương ứng; không sửa filter feed hiện tại.
- Không khôi phục các action đã bị xóa hoặc snapshot quá khứ từ database cuối cùng.
- SQL không cập nhật/xóa dữ liệu đã import, không phát notification hoặc index vector.
- Không làm thêm PPR, LightGCN, policy selection, benchmark hay tự chạy import database.

Working tree có sẵn file untracked `../nul` trước khi bắt đầu; không đụng đến file đó.
Phần adapter dừng ở phạm vi này. Các task moderation/cold-start được thực hiện trên
nhánh riêng từ main, không mang theo code OASIS.
