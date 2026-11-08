# Gate HTTP adaptive schema2 — 08/11/2026

Lifecycle/SQL/HTTP hoạt động trên artifact thật 250 user; direct latency đạt gate
lab. Gateway gate không đạt do giới hạn150 request/phút. Đây là hai kết quả riêng;
không thể suy ra hệ thống đã sẵn sàng nhận tải production hoặc có lợi ích relevance.
Primary practical relevance gate của nghiên cứu vẫn FAIL.

## Thiết lập và đầu vào

Runner: `tools/oasis-smoke/verify.ps1`, `Program.cs`, `AdaptiveFeedChecks.cs` và
`AdaptiveGatewayProbe.cs`. PostgreSQL15/Redis7/NGINX chạy container test riêng;
API là tiến trình con Kestrel, port được bind động. Cleanup chỉ tác động process/
container của từng lượt. Docker Desktop được khởi động vì engine ban đầu chưa chạy.
Không đổi cấu hình gateway production, feature flag production, model hoặc seed.

Nguồn import: `.tools/datasets/adaptive-confirmatory-main-20261111/export`.
Import toàn native snapshot5k user, kiểm toàn bộ GUID/field/FK, rollback khi
inject FK sai và SQL replay không đổi dữ liệu. Artifact chỉ chứa250 user đã chọn
theo source ID tăng dần, không phải full5k serving. Artifact11867251byte, SHA256
`7120ed20ae9c8db81bf897411be349581bdc2d6e297f613f04a9986af0a43824`.
B1/R đều EASElambda1000, ngưỡng lần lượt1/5. Không refit/retune trong milestone.

Máy: AMD Ryzen9 7940HS,8core/16logical CPU, Windows build26200. Smoke runner
.NET10.0.10; API targetnet9.0, runtime9.0.18 có trên máy. Các tiến trình build
MSBuild cũ được giữ nguyên. Lab này không có cô lập CPU hoặc kiểm soát nhiệt/điện;
không xem các lần chạy là benchmark phần cứng độc lập.

## Hành vi đã quan sát

Fresh run cuối `a82952222e0243599448477ec5b72e08` PASS113848 kiểm tra
PostgreSQL/import/HTTP. Phần lớn là xác nhận từng record import, không phải
113848 HTTP requests; số request lifecycle và tải được lưu riêng trong summary.
Runner bỏ catalog HTTP walk cũ của schema1 khi kiểm schema2, vẫn giữ kiểm tra
database import đầy đủ. Schema1 và mặc định tắt giữ đường kiểm tra cũ.

- HTTP401 khi chưa xác thực;400 cho method/pagination sai; version artifact giữ
  nguyên, order theo rank/GUID đúng qua trang, lặp trang ổn định, trang cuối rỗng.
- Current positives0→5 chuyển B1/R đúng ngưỡng; Like+save cùng post không tăng
  đếm hai lần. Năm saves riêng cũng đạt ngưỡng; gỡ hết Like/Love trở lại cold.
  Đây là positive vận hành Like/Love UNION save, rộng hơn offline likes-only.
- Truy vấn UNION và hydration chạy thật trên PostgreSQL; các gate này vượt qua
  giới hạn EF InMemory của milestone loader trước. Không claim parity mọi SQL.
- NSFW, post/profile private, archive/delete, own post, author/viewer ban và
  current positives được loại. Khi toàn bộ rank không an toàn, source chuyển
  empty-model-fallback và không đưa lại các ID unsafe để lấp trang.
- User mới dùng current cold topics; item food/art mới trong database xuất hiện
  tức thì khi chọn/đổi topic. Topic PUT thiếu idempotency key bị400; replay key
  giữ version1; stale version409 và không ghi đè winner; chọn[] về general.
- User mới đủ positive nhưng thiếu row model có missing-model-fallback. Method
  legacy ease trên schema2 cũng missing-model-fallback, không alias B1/R.
- Artifact file thiếu hoặc checksum sai khiến tiến trình từ chối trước khi listen,
  không khởi động với model giả. Đây là fail-closed startup, khác missing user row.

Fixture vận hành reset positives của một viewer có rank trong database test rồi
thêm tuần tự; không thay dữ liệu nghiên cứu gốc. Các thay đổi visibility, topic,
bài mới và saves chỉ nằm trong database bị hủy cuối lượt. Artifact250user hiện
không có row R rỗng; row không có learned history vẫn được kiểm bởi unit fixture,
không gọi đó là bằng chứng HTTP trên row rỗng của artifact thật.

## Tài nguyên và transport

Mỗi method chạy10concurrent,100warmup+1000measured, pageSize10. p95 nearest-rank
là mẫu950/1000 sau sort. Mọi direct request trong warmup/measurement phải200,
đúng source model, không có current positives hay NSFW.

| Transport | B1 | R | Kết luận |
|---|---:|---:|---|
| Direct p95 |57.2082ms|57.4861ms|PASS ngưỡng100ms trên fixture này|
| NGINX measured status |1000×429|999×429,1×200|FAIL gateway load gate|

Gateway là proxy lab một upstream, giữ rate150/minute và burst30; hai method
dùng chung bucket theo IP và không reset giữa chúng. Không tăng rate/burst hoặc
bypass limiter để tạo PASS. p95 tổng response gateway khoảng1–2ms chủ yếu là429,
không phải p95 feed thành công. Số200 quá ít để kết luận throughput/latency gateway.
Không kiểm TLS, failover hai replica, SignalR hoặc toàn bộ routing production.
Gate tổng serving/resource chưa thể ghi PASS chỉ vì direct đạt.

Fresh run có5 PID khởi động riêng, start→health2422.4788–3073.2132ms. Snapshot
được startup validator load trước health; timing còn gồm host/DB/JIT, không phải
parse artifact thuần. Peak working set lúc ready163737600–164724736byte;
tiến trình cuối qua toàn workload đạt258326528byte. Đây là resident process RAM,
không phải managed heap, model-state bytes, peak toàn stack hay original training
RAM. Không claim p99 từ5mẫu hoặc cold disk cache;5process restart không xóa OS cache.

Hai lượt phát triển trước được giữ nguyên trong archive: run001 direct p95
B1/R39.8814/41.1029ms; run00247.0626/49.2153ms. Không chọn số thấp nhất làm
kết quả chính. Run cuối có hash source/binary trong run.json và là bằng chứng
chính cho phiên bản commit này. Các lượt trước chưa ghi đủ inventory đó.

## Tái lập và giới hạn còn lại

```powershell
./tools/oasis-smoke/verify.ps1 -Python .tools/oasis-env/Scripts/python.exe -ExistingRunDirectory .tools/datasets/adaptive-confirmatory-main-20261111 -ExperimentalArtifact .tools/feed-experiment/adaptive-export-20261108-main11-250/artifact.json -SkipToolTests
```

`SkipToolTests` chỉ bỏ suites Python đã kiểm ở milestone trước, không bỏ build,
import/FK/replay/HTTP. Mỗi lượt tạo run directory và Compose project mới. Sử dụng
artifact stale copy trong provenance để kiểm stale mode; không ghi đè artifact gốc.
Raw HTTP,1000mẫu/method/transport, API logs/restarts, startup failures, nguồn/binary
hashes và cleanup status nằm trong archive được đọc lại từng member. Chỉ summary,
manifest và báo cáo được đưa Git; raw dataset/model tiếp tục ở `.tools`.

Full original search/training/refit/export/load cost và peak process RAM của
pipeline nghiên cứu vẫn chưa đầy đủ. Export refit0.365270s/model-state32521008byte
không thay thế chúng. Không có retune, thay source/seed/gates, merge hoặc deploy.
Không đổi primary practical FAIL, các robustness âm hoặc kết luận synthetic-only.

## Stale, regression và lưu bằng chứng

Stale run `23c76fb1b6f8485899f9d99ee5a43912` PASS113757 kiểm tra. Copy chỉ đổi
createdAtUtc thành `2026-11-07T01:40:50.7272226+00:00`; tất cả field model/GUID
được so sánh lại bằng canonical JSON và giữ nguyên. SHA256 copy
`97b0351334de9116b3b5f4de41c24d35d9434900d28697044786c8c675507f53`.
B1 từ positive1 và R từ5 dùng stale-model-fallback; dưới ngưỡng vẫn current cold.
Stale copy cũng qua5process starts và hai negative startup probes. Không chạy
warm-model load gate trên stale fallback rồi gắn nhãn B1/R inference.

23focused .NET regression tests PASS; build0errors. Cảnh báo NU1903 SignalR và
EF relational version conflict có từ trước vẫn còn; không cập nhật dependency.
`git diff --check` áp dụng phần thay đổi mới; không sửa lịch sử/blank EOF cũ.

Máy đọc: [summary.json](summary.json). Archive57members/1328928byte,
`.tools/dataset-archives/adaptive-http-20261108.zip`, SHA256
`98559218e45e2a840a14e36b19668d6d3aa8aa820d8607797e3927395fd5ed41`.
Mỗi member đã đọc lại và so hash/size. Có4run reports, raw API/checks/load,
metadata CPU/RAM/image digests và script tạo summary/archive. Chỉ mới fresh/stale
run cuối được dùng cho inventory phiên bản chính; hai lượt trước là phát triển.
Không còn API worker hoặc container test sau cleanup; Docker Desktop được giữ mở.
