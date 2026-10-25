# Cold-start feed và onboarding chủ đề

Plan: [COLD_START_PLAN.md](COLD_START_PLAN.md). Branch độc lập từ main, không cần
OASIS hoặc moderation branch. Không có training hay graph engine chạy giả trong backend.

## Hành vi

Cold user là user chưa có Like/Love trên post và chưa có post trong collection
của mình. Comment, follow hoặc reaction khác chưa chuyển user sang warm. Sau
positive đầu tiên, cả /feed và /feed-with-reposts trở về luồng warm hiện tại.

- Chưa chọn chủ đề hoặc chọn []: round-robin 10 chủ đề general.
- Chọn 1–10 chủ đề: round-robin những chủ đề đó trước; khi thiếu mới bù catalog chung.
- Dedup post nhiều tag, ưu tiên tối đa hai post/tác giả trong lượt đầu. Khi thiếu,
  nới giới hạn tác giả để vẫn có feed. Đây là ưu tiên mềm trong pool hữu hạn.
- Ties cố định theo PostId; không random lại khi lật trang.
- Chỉ dùng post public của profile public, không ban đang hiệu lực, không NSFW,
  không archived/soft-deleted, không phải bài của chính viewer.
- Lọc lại trạng thái hiện tại khi hydrate, giữ thứ tự recommendation rồi mới
  phân trang. Cold /feed-with-reposts chỉ có item Post, không trộn repost/time-sort.

## API dùng lúc thiết lập tài khoản

1. `GET /api/discovery/topics`: mảng topic code để frontend hiển thị.
2. Sau đăng nhập: `GET /api/profiles/me/interests`.
3. `PUT /api/profiles/me/interests` với JWT và `Idempotency-Key`.
4. Gọi `/api/posts/feed` hoặc `/api/posts/feed-with-reposts` như trước.

Response khi chưa chọn:

```json
{"topics": [], "version": 0}
```

Body PUT đầu tiên:

```json
{"topics": ["travel", "food"], "version": 0}
```

PUT thành công trả topics đã normalize và version mới. Lần cập nhật tiếp theo
gửi version từ GET/PUT gần nhất; stale version trả 409 CONCURRENCY_CONFLICT.
Dùng `topics: []` để trở lại general. Array null/unknown topic/quá 10 entry trả
400, profile thiếu trả 404. UserId luôn lấy từ JWT, không nhận từ body.
Normalize trim + lowercase + dedup; tối đa 10 input entries trước dedup.
Page >= 1, size/pageSize 1–100; ApiController trả 400 khi sai range.

Catalog hiện tại:
travel, food, sports, city, nature, technology, lifestyle, art, fitness, music.
Tag bài đăng phải khớp code sau lowercase; không suy luận chủ đề từ caption/ảnh.
Thêm topic bằng `Auth/Domain/InterestTopics.cs`, đồng thời chuẩn hóa tag seed.

## Storage và ranh giới module

Auth sở hữu aggregate `ProfileInterests` và bảng cùng tên:

| Column | Kiểu | Invariant |
|---|---|---|
| ProfileId | uuid, PK/FK Profiles.Id | Một hàng/profile, cascade khi xóa profile |
| Topics | text[], NOT NULL | 0–10 topic, kiểm tra catalog trong domain |
| Version | integer, NOT NULL | Bắt đầu 1; EF concurrency token |

Profile chưa có hàng được đọc thành topics=[]/version=0. Không đổi DTO Profile
hay registration API; onboarding là PUT riêng sau login. Command dùng aggregate
tracking qua store, TransactionBehavior và SaveChanges. Concurrent insert/update
được chuyển thành 409, không ghi đè bên thắng. Không có side-effect/event cần
Outbox vì preference được đọc trực tiếp mỗi request, chưa cache hay replicate.

ContentDiscovery chỉ có contract/ranker. Host adapter kết nối query Auth qua
MediatR, không thêm project reference giữa Auth và ContentDiscovery. Read path
dùng AsNoTracking/projections, không load toàn bộ comment hoặc query quyền từng post.
DI nằm trong AddAuthModule/AddContentDiscoveryModule, không sửa Program.cs.
Gateway/rate-limit và HTTP IdempotencyMiddleware hiện có giữ nguyên.

## Điểm nối graph engine tương lai

Contract tạm thời: `IGraphCandidateSource.GetAsync(userId, topics, limit, ct)`
trả `IReadOnlyList<GraphCandidate>`; mỗi candidate chỉ cần PostId GUID và Score.
Adapter thật phải dịch ID index/string của engine sang PostId của Favi, và chuyển
score về finite double. Không đưa entities/DbContext của engine vào module.

Hiện `NoGraphCandidateSource` trả []. Đây là fallback explicit, không phải dự đoán
giả. Khi engine có thật, cần thay **hai chỗ**:

1. Viết class host implement IGraphCandidateSource để map HTTP/file/output thật.
2. Thay registration trong ContentDiscoveryModuleExtensions.cs.

Nếu engine contract đổi, ưu tiên giữ contract nhỏ này và sửa adapter. Chỉ đổi
IGraphCandidateSource/ranker nếu semantics thực sự cần thay. Không giả định route
HTTP, port, tên field hoặc model version của engine chưa xây.

Host giới hạn graph 300 IDs, timeout 250 ms; timeout/HTTP lỗi chuyển catalog.
Caller cancellation propagate. Adapter phải parse/validate response lỗi thành
HttpRequestException; không nuốt cancellation. Favi lọc lại IDs với database;
score non-finite, ID empty, duplicate, unknown/unsafe/private post không được tin.
Engine scores được đổi thành thứ tự ưu tiên, không cộng raw score khác thang đo
với popularity. Topic round-robin vẫn áp dụng cho user cold.

## Tài nguyên và phân trang

- Mỗi topic tối đa 20 ứng viên, catalog general tối đa 60, graph tối đa 300.
- Candidate sort catalog: reaction count + 2*comment count, rồi CreatedAt, PostId.
- Tối đa 10 truy vấn topic nhỏ + một general pool; không query vô hạn toàn catalog.
- Ranker cuối tối đa 300 post. Hydration dùng projection media/tag và COUNT comment,
  không load nội dung comments hay N+1 PrivacyGuard cho từng post public.
- Không dùng Redis/cache mới hoặc gọi model trên request. Graph mặc định không network.
- TotalCount là số post hợp lệ trong cửa sổ candidate, không phải toàn database.
- Cùng dữ liệu có thứ tự ổn định; nếu có tương tác/bài mới giữa các request thì thứ
  tự có thể đổi. Chưa có feed-session snapshot/cursor cố định.
- Tài nguyên cạn/ít author/ít topic thì diversity giảm; không bịa thêm bài để đủ trang.

## Migration và cách chạy

Gate mở rộng ngày 25/10/2026 đã pass 19 unit + 67 architecture/privacy và 109
checks mỗi schema. Xem [release review](COLD_START_RELEASE_REVIEW.md) cho quy trình
CI, local merge, rollback code/schema và điều kiện production còn thiếu.

Gate backend tái lập: `tools/cold-start-smoke/verify.ps1`, chạy CLI ngoài sandbox.
Bao gồm unit/architecture, hai schema upgrade → rollback → re-upgrade và HTTP
registration/interests/feed/Like/Love/save, concurrent insert/update, gỡ positive
cuối cùng trở lại cold. Script dừng stack riêng trong finally và lưu pass/fail.
Rollback schema drop ProfileInterests nên mất preferences; rollback code nên giữ
schema additive. Nếu Down thật cần backup/restore preference riêng. Đây không
thay thế kiểm thử k6 qua NGINX nhiều replica hoặc frontend trước phát hành.

Runner smoke hỗ trợ `--upgrade` để tạo database riêng, apply migrations tới mốc
cuối main, thêm dữ liệu legacy rồi nâng cấp và so sánh field trước/sau. Runner cũng
kiểm tra cold → warm cho Like/Love/save qua HTTP ở cả hai feed bằng own-post marker.
Xem README smoke để chạy bằng CLI ngoài sandbox; kết quả chỉ có giá trị khi exit 0.

Ngày 25/10/2026 cả `--upgrade` và `--upgrade --existing-columns` pass 82/82 checks
mỗi lượt. Fixture legacy profile/post giữ nguyên các field cũ; Profile.Version=7
không đổi, Posts.Version thiếu được thêm 1 hoặc có sẵn 9 được giữ nguyên. Migrate
lần hai không đổi dữ liệu post. Like/Love/save qua HTTP chuyển đúng cả hai feed.
Đây là hai biến thể schema trên database test riêng, không phải bản sao dữ liệu
ứng dụng hoặc kiểm chứng mọi loại drift. Không sửa production trong lượt này.

Migration `20261024201327_AddProfileInterests` thêm bảng mới. Khi generate phát
hiện drift có sẵn trong main: Version trên Collections/Comments/Conversations/
Messages/Posts/Reposts/Stories, UpdatedAt trên Conversations/Stories và hai index
Posts chưa được snapshot mô tả. Migration đồng bộ prerequisite này theo cách
ADD COLUMN/CREATE INDEX IF NOT EXISTS, Version mặc định 1, UpdatedAt mặc định NOW().
Không drop index/cột cũ. Down chỉ drop ProfileInterests; giữ prerequisite vì code
main đã cần chúng và chúng có thể đã tồn tại trước migration.

`AppDbContextFactory` giúp EF tooling không khởi động host, Redis hoặc seed.
Connection design-time lấy ConnectionStrings__Default nếu có, còn lại placeholder;
generate/script/check-model không kết nối database. Update database là thao tác thật.

```powershell
dotnet ef database update --project Favi-BE.API/Favi-BE.API.csproj --startup-project Favi-BE.API/Favi-BE.API.csproj
dotnet test Favi-BE.UnitTests/Favi-BE.UnitTests.csproj
dotnet test Favi-BE.ArchitectureTests/Favi-BE.ArchitectureTests.csproj
```

Hai csproj tồn tại trong thư mục API nên phải chỉ rõ filename như trên. Đặt
ConnectionStrings__Default trỏ đúng database local trước database update. API startup
migration cũng nhận migration mới theo advisory lock có sẵn. Không tự chạy update
database trong phiên triển khai code này.

Đã có thêm [HTTP smoke test](../tools/cold-start-smoke/README.md): 40 kiểm tra
trên Kestrel + PostgreSQL 15 + Redis 7 thật; migrations và truy vấn thực đã chạy.
Có kiểm tra cập nhật đồng thời, isolation giữa user và Redis replay. `Seeding:Enabled=false`
chỉ dùng để bỏ benchmark seed khi test fixture, mặc định true giữ startup cũ.
Đã sửa version increment kép cho ProfileInterests: aggregate sở hữu version,
DbContext bỏ qua tăng generic cho aggregate này. Reader regression test kiểm tra
update tăng đúng một lần. Các kiểm tra InMemory trước đó vẫn giữ lại.

Engine, frontend onboarding và integration load test là việc tiếp theo; không
thuộc phần backend KISS này. Branch độc lập moderation nên chỉ biết IsNSFW legacy,
không bảo đảm pending image đã được kiểm duyệt hoặc violence được phát hiện trên main.
