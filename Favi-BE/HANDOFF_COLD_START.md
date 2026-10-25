# Handoff cold-start backend

Trạng thái mới nhất: squash cold-start từ feature `7859ded` vào main local;
không push/deploy, không tích hợp OASIS/moderation. Các phần kiểm chứng trước đó
giữ theo thời điểm chạy. Gate chính thức và điều kiện rollout trong release review.

## Flow review → test → tích hợp

Gate backend mới chạy bằng `tools/cold-start-smoke/verify.ps1`: 19 unit, 67
architecture/privacy và 109 checks mỗi schema upgrade/rollback/re-upgrade đã pass.
Đã thêm concurrent UPDATE, Haha không đổi warm và gỡ positive cuối trở lại cold.
Workflow CI và quy trình squash merge local/rollback được mô tả đầy đủ trong
[release review](document/COLD_START_RELEASE_REVIEW.md). Không đồng nghĩa đã đạt
production rollout: còn gateway/load, frontend/staging, peer review/remote CI và
cảnh báo dependency baseline. Không push/deploy trong flow này.

## Gate trước merge: upgrade dữ liệu và cold → warm

Ngày 25/10/2026 bổ sung runner `--upgrade` và `--upgrade --existing-columns`.
Cả hai lượt pass **82/82 checks**, exit 0 trên PostgreSQL/Redis/Kestrel thật.
Mỗi lượt gồm 5 checks migration + 77 HTTP/status/behavior checks, có 40 checks cũ.
Không cộng thành 164 trường hợp độc lập vì hai lượt chạy cùng suite ở hai schema.

- Nâng cấp từ migration cuối main `20260423000000_AddUpdatedAtAndVersionToProfile`
  sau khi đã có profile/post legacy. Các migration trước đó không đổi so với main.
- So sánh JSON tất cả field cũ của hai hàng fixture trước/sau: giữ Unicode,
  timestamps, NSFW và Profile.Version=7; post thiếu Version được backfill 1.
- Schema có sẵn Posts.Version=9 và index cùng tên: nâng cấp thành công, giữ Version=9.
- Không tạo interests mặc định hàng loạt; migrate lần hai không đổi dữ liệu post.
- Like, Love, save đầu tiên qua HTTP ở ba account độc lập chuyển cả `/feed` và
  `/feed-with-reposts` từ cold sang warm, chứng minh bằng own-post marker.

Chỉ thay runner/tài liệu, không sửa code production. Fixture không phải database
ứng dụng có dữ liệu thực, không khẳng định mọi schema drift tùy ý đều an toàn.
Chưa kiểm tra rollback migration, NGINX/frontend hoặc tải lớn. Lệnh tái lập trong
README smoke; report/log nằm ở `.tools/cold-start-upgrade/` và
`.tools/cold-start-upgrade-existing/`. Stack test được dừng sau kiểm tra.

## Kiểm chứng độc lập ngày 25/10/2026

Đã chạy lại trên commit `960e9af`: 19/19 unit, 67/67 architecture/privacy và
40/40 HTTP checks trên Kestrel + PostgreSQL 15.19 + Redis 7 thật, database test
riêng `favi_cold_smoke`. Không tái xuất hiện lỗi version tăng kép. Không sửa code.
Đã kiểm tra riêng moderation `e17ae65` (10 Python + 11 .NET + 10 HTTP checks với
weights thật) và OASIS `b325c8e` (12 Python tests). Xem báo cáo đầy đủ tại
[document/INDEPENDENT_FEATURE_VERIFICATION.md](document/INDEPENDENT_FEATURE_VERIFICATION.md).
Moderation đạt mức service/client, chưa accuracy hoặc lifecycle upload/worker/feed;
OASIS chưa simulator/PostgreSQL import thật. Không merge/push, giữ nguyên `../nul`.

Branch `feat/cold-start-feed`, base main `155d6be`.
Nhánh được tạo lại từ main, không mang commit OASIS hoặc moderation.

## Cập nhật kiểm tra chức năng qua HTTP

Sau handoff ban đầu đã chạy smoke trên PostgreSQL 15 + Redis 7 + Kestrel thật.
40 kiểm tra pass, gồm JWT qua registration thật, cả hai feed, lựa chọn 1–n topic,
reset general, replay, conflict và hai cập nhật đồng thời. Xem
[tools/cold-start-smoke/README.md](tools/cold-start-smoke/README.md).
Đã phát hiện và sửa version increment kép, thêm regression test; tổng unit tests
hiện 19. Migration đã apply thành công trên database test riêng, không phải database
Favi đang dùng. Chưa kiểm tra frontend/NGINX hoặc tải lớn.

## Những gì đã làm

- [Plan](document/COLD_START_PLAN.md) và [spec/cách chạy](document/COLD_START_FEED.md).
- Onboarding backend: GET catalog 10 topic, GET/PUT interests của chính user.
- Auth aggregate/store/commands/queries cho topics và optimistic concurrency.
- Bảng ProfileInterests với một hàng/profile, text[] topics và version; profile
  chưa chọn được đọc thành []/version=0, không phải tạo row mặc định hàng loạt.
- Cold-start general đa topic hoặc ưu tiên 1–10 topic đã chọn, dedup, ưu tiên
  đa tác giả, fallback khi ít nội dung. Warm user giữ feed cũ sau Like/Love/save.
- Nối cả /feed và /feed-with-reposts; giữ ranking, không time-sort lại cold feed.
- Lọc public/safe/active và hydrate bằng projection, COUNT comment, tránh N+1.
- Seam graph engine với PostId/Score và cancellation, graph timeout/HTTP error
  dùng catalog; default rõ ràng chưa có engine, không trả fake predictions.
- Migration cộng thêm bảng và đồng bộ baseline EF drift bằng lệnh bổ sung an toàn.
- EF design-time factory tách tooling khỏi startup Redis/seed/database initialization.

## Cách dùng từ frontend hoặc API client

1. GET `/api/discovery/topics` để lấy code.
2. Đăng nhập rồi GET `/api/profiles/me/interests` để lấy topics/version.
3. PUT cùng route, JWT + `Idempotency-Key`, body:

```json
{"topics":["travel","food"],"version":0}
```

4. GET `/api/posts/feed?page=1&pageSize=10` hoặc feed-with-reposts như cũ.

Version=0 chỉ dùng lần đầu chưa có row. Cập nhật gửi version trả về gần nhất;
409 thì GET lại trước khi retry với idempotency key mới. `topics: []` là general.
Frontend onboarding chưa được viết trong task backend này.

## Thay graph engine ở đâu

| File | Vai trò |
|---|---|
| Modules.ContentDiscovery/Application/Contracts/IGraphCandidateSource.cs | Contract tạm, ID GUID + finite score |
| API/Application/ContentDiscovery/NoGraphCandidateSource.cs | Stub trả [] vì engine chưa có |
| API/DependencyInjection/ContentDiscoveryModuleExtensions.cs | Thay registration bằng adapter thật |
| API/Application/ContentDiscovery/ColdStartQueryReader.cs | Eligibility, candidate fusion và fallback |
| Modules.ContentDiscovery/Application/ColdStart/ColdStartRanker.cs | Trộn topic, dedup và ưu tiên tác giả |

Các path trên tính từ thư mục chứa solution, có prefix `Favi-BE.` tương ứng với
tên project. Engine thực có thể khác route/schema/ID mapping. Implement adapter
ở host để đổi format, đừng sửa Auth/domain hay cho module reference engine assembly.
Nếu graph output dùng integer index, đổi về PostId GUID từ mapping snapshot.
Các test có FakeGraph chỉ phục vụ kiểm tra contract, không đăng ký vào runtime.

## Database

Migration `20261024201327_AddProfileInterests` đã apply thành công trên PostgreSQL
test riêng qua smoke runner. Chưa apply lên database ứng dụng của bạn.

Đã phát hiện model main thiếu migrations/snapshot cho một số Version/UpdatedAt
và index Posts. Migration dùng ADD COLUMN/CREATE INDEX IF NOT EXISTS, không drop
cột/index cũ. Down chỉ drop ProfileInterests, giữ baseline prerequisite. Chi tiết
và danh sách cột nằm trong spec; review script trước khi áp dụng lên database có dữ liệu.

```powershell
dotnet ef database update --project Favi-BE.API/Favi-BE.API.csproj --startup-project Favi-BE.API/Favi-BE.API.csproj
```

Đặt ConnectionStrings__Default đúng database local trước lệnh update. Không dùng
lệnh rút gọn chỉ thư mục API vì ở đó có hai csproj. Không có lệnh xóa/reset dữ liệu
được chạy trong phiên làm việc này.

## Kiểm tra

- API build thành công.
- 19/19 unit tests pass: 8 ranking/domain/MediatR mới, 5 reader integration bằng
  EF InMemory mới và 6 tests domain có sẵn.
- 67/67 architecture/privacy tests pass.
- EF has-pending-model-changes: không còn drift.
- Generate idempotent SQL script thành công; kiểm tra Up chỉ bổ sung, không DROP.
- git diff --check pass.

Reader tests kiểm tra lọc NSFW/private/banned/deleted/archive, lựa chọn topic,
warm transition, recheck visibility sau khi đổi privacy, graph ID không hợp lệ
và HTTP fallback. EF InMemory không chứng minh PostgreSQL SQL translation hoặc
concurrent transaction thực. Vì vậy đã bổ sung 40 kiểm tra HTTP với Kestrel,
PostgreSQL và Redis thật, gồm migration và optimistic concurrency.

## Giới hạn KISS

- Cold = chưa có positive hiện tại; chưa có chuyển tiếp mềm theo số tương tác.
- Catalog topic cố định, match tag lowercase, không dùng NLP hoặc CLIP để đoán topic.
- Tối đa 20 candidate/topic + 60 general + 300 graph; rank window tối đa 300.
- General/selected diversity phụ thuộc catalog sẵn có; thiếu thì nới cap và bù chung.
- Không cache mới, không model training, không queue hay background job mới.
- Không feed-session snapshot, ranking có thể đổi khi dữ liệu thay đổi giữa các trang.
- Không bổ sung frontend/registration form; PUT preference riêng sau login.
- Không merge moderation; nhánh này chỉ dựa vào IsNSFW hiện có trên main.
- Preference không phát integration event vì chưa có side-effect/projection khác;
  mọi request đọc lại preference, không cần invalidation.

## Trạng thái các nhánh để tiếp tục

| Nhánh | Nội dung | Trạng thái |
|---|---|---|
| feat/oasis-adapter | Offline OASIS adapter | b325c8e, đã push origin |
| feat/pretrained-image-moderation | Shared encoder + NSFW/violence routing | e17ae65, commit local |
| feat/cold-start-feed | Task trong handoff này | Source 7859ded, squash vào main local |

Handoff moderation nằm trên nhánh moderation, xem không cần checkout bằng:

```powershell
git show feat/pretrained-image-moderation:Favi-BE/HANDOFF_PRETRAINED_MODERATION.md
```

Runtime Python/model cache trong `.tools` bị ignore, không commit weights. File
untracked `../nul` có sẵn từ đầu được giữ nguyên. Main không nhận commit task nào.
