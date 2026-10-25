# Kiểm chứng độc lập ba feature — 25/10/2026

Trạng thái mới: cold-start được tích hợp riêng vào main local bằng squash từ
`7859ded`; không push/deploy. Gate mở rộng 19 unit + 67 architecture/privacy và
109 checks mỗi schema đã pass trước tích hợp. Xem COLD_START_RELEASE_REVIEW.md.
Các mục dưới mô tả các lượt trước đó, khi ba nhánh còn độc lập.

## Bổ sung gate cold-start trước merge

Sau lượt kiểm chứng ban đầu, đã mở rộng runner/tài liệu (không sửa production):
`--upgrade` và `--upgrade --existing-columns` đều exit 0, pass 82/82 checks mỗi lượt
(5 migration/database + 77 HTTP/status/behavior). Migrate từ mốc cuối main với
profile/post legacy rồi so sánh JSON field trước/sau. Giữ Profile.Version=7,
backfill Posts.Version=1 khi thiếu; giữ 9 khi đã có và chấp nhận index cùng tên.
Ba account độc lập thực hiện Like/Love/save qua HTTP: own post bị loại khi cold,
xuất hiện khi warm trên cả hai feed. Không tạo interests mặc định hàng loạt.
Đây là fixture nâng cấp, không phải database ứng dụng thực; chưa kiểm tra rollback,
NGINX/frontend/load. Xem handoff cold-start và README smoke để tái lập.

Kiểm tra trực tiếp từng nhánh, không merge/push. Các commit được kiểm tra:
`feat/cold-start-feed` tại `960e9af`, `feat/pretrained-image-moderation` tại
`e17ae65`, `feat/oasis-adapter` tại `b325c8e`. Kết thúc trở về nhánh cold-start.
Không sửa mã nguồn vì chưa phát hiện lỗi chức năng mới trong phạm vi kiểm tra.

## Kết quả chạy lại

| Feature | Kết quả độc lập | Phạm vi bằng chứng |
|---|---|---|
| Cold-start | 19/19 unit, 67/67 architecture/privacy, 40/40 HTTP checks | Kestrel, PostgreSQL 15.19 và Redis 7 thật, migration trên database test riêng |
| Moderation | 10/10 Python, 11/11 .NET, 10/10 HTTP checks | FastAPI standalone, weights CPU thật từ cache offline, NSFWService .NET thật |
| OASIS | 12/12 Python tests | Fixture SQLite, validation, mapping, graph export và SQL replay trên SQLite stand-in |

Chỉ ghi nhận HTTP smoke khi process thoát 0. Không sử dụng report cũ để suy ra pass.
Một lần discover Python khi chưa chuyển được branch chạy 0 tests không được tính.
Các bộ .NET có test dùng chung; không cộng các con số thành số test mới độc lập.

### Cold-start thực sự hoạt động

Runner dùng duy nhất `favi_cold_smoke`, user `favi_smoke`, PostgreSQL loopback
15439 và Redis 16389 trong Compose project `favi-cold-start-smoke`. Không dùng
database ứng dụng. Fixture chứa ba topic và các bài public/private/NSFW; đăng ký
tài khoản thật qua HTTP để lấy JWT. NSFW service và vector index bị tắt trong
smoke này, nên đây không phải kiểm chứng tích hợp hai feature.

40 checks xác nhận catalog, self interests, general diversity, selected/multiple
topics, reset general, cả hai endpoint feed, loại private/NSFW, pagination,
validation, thiếu idempotency key, Redis replay, stale version và concurrent
first insert chỉ có một 200 và một 409. Chuỗi cập nhật version 0 → 1 → 2 → 3
thành công; regression version tăng kép không tái xuất hiện.

Runtime đăng ký `NoGraphCandidateSource`: graph trả rỗng và dùng catalog fallback.
Chưa có graph engine hoặc kết quả đánh giá thuật toán recommendation.
Migration chạy trên database test mới; chưa kiểm chứng nâng cấp database ứng dụng
có dữ liệu. Không kiểm tra frontend, NGINX hoặc tải lớn.

### Moderation sẵn sàng nối ở mức service/client

Service preload xong encoder và cả hai head trước khi báo startup complete.
`GET /nsfw/ready` trả thành công; POST inference có cả NSFW/violence scores và
decision. .NET NSFWService thật đọc đúng kết quả; ảnh lỗi, batch có ảnh lỗi,
request rỗng, threshold sai và network failure đều có hành vi phù hợp smoke.

Ảnh fixture `sample.jpg` đã có sẵn được tái sử dụng. Kết quả chạy lại:
NSFW `0.01939053093457468`, violence `0.23382411680331214`, decision `review`,
`is_nsfw=true`, model version `3f9f96cb90da:b88fe03369e3`.
Đây là bằng chứng inference/kết nối/policy; không phải accuracy, calibration
hoặc benchmark latency. Một ảnh thường bị review cho thấy cần bộ ảnh gán nhãn
độc lập và đo false positive/negative trước khi kết luận chất lượng.

Nối application bằng `NSFW__Enabled=true`, `NSFW__BaseUrl` tới service mà host
truy cập được và `NSFW__TimeoutSeconds=30`; restart API để đọc cấu hình.
Docker dùng DNS service, không dùng loopback container .NET. Router standalone
được dùng chung với vector API, nhưng lượt này chưa chạy image Docker toàn stack.

Chưa kiểm chứng lifecycle upload → publish task → worker → cập nhật IsNSFW → feed.
Mã worker hiện có gọi NSFWService và cập nhật cờ, nhưng không có publication gate
cho trạng thái pending. Không được suy từ smoke này rằng mọi ảnh mới đều đã được
kiểm duyệt trước khi hiển thị. Chưa có workflow manual review; review/reject cùng
được ánh xạ vào IsNSFW legacy. Caption không được mô hình ảnh kiểm duyệt.

### OASIS

Chạy lại trực tiếp trên nhánh adapter bằng Python standard library: 12 tests pass.
Không có database OASIS sinh bởi simulator thật trong lượt này. Chưa chạy OASIS/LLM,
chưa import SQL lên PostgreSQL thật, chưa chứng minh schema simulator thực tế khớp
fixture. SQL replay trong tests vẫn là SQLite stand-in, dù cold-start có PostgreSQL
riêng đang hoạt động. Không đánh đồng hai bằng chứng này.

## Phát hiện theo mức độ

- **P1 — giới hạn tích hợp đã biết:** pending publication chưa được chặn xuyên suốt.
  Service/client có thể nối, nhưng chưa đủ căn cứ tuyên bố toàn bộ ứng dụng an toàn
  trước khi worker hoàn tất. Không mở rộng task sang thiết kế lifecycle mới.
- **P2 — chất lượng mô hình chưa được đo:** threshold 0.2/0.7 chưa calibration trên
  ảnh Favi. Không dùng số checks pass làm chỉ số accuracy.
- **P2 — cảnh báo dependency có sẵn:** build báo NU1903 cho
  `Microsoft.AspNetCore.SignalR.Common` 9.0.11 và MSB3277 cho EF Core Relational
  9.0.1/9.0.9 trong test projects. Tests vẫn pass; không thay dependency ngoài scope.
- **P2 — khoảng trống bằng chứng OASIS:** chưa simulator thật/PostgreSQL import thật.
  Chưa tìm thấy lỗi adapter mới qua tests và rà soát phạm vi nêu trên.

## Cách tái lập và môi trường

Theo `tools/cold-start-smoke/README.md` và README moderation trên nhánh tương ứng.
SDK .NET 10.0.302; API target net9.0. Python moderation dùng
`.tools/moderation-env/Scripts/python.exe`; OASIS dùng
`.tools/python/cpython-3.11.14-windows-x86_64-none/python.exe`.
Moderation dùng HF_HOME `.tools/hf-cache`, HF_HUB_OFFLINE=1 và seed root
`.tools/moderation-fixtures`; không tải lại fixture hay weights.

Sandbox chặn Docker API và ghi `.git/index.lock`; chạy CLI ngoài sandbox cho
branch switching và các service/network checks. Theo yêu cầu người dùng, các
lệnh .NET tiếp theo chạy ngoài sandbox bằng CLI, không dùng remote control/GUI.
Build smoke client moderation đầu tiên thiếu project.assets.json; restore/build
thành công rồi mới chạy DLL. Đây là lỗi chuẩn bị môi trường, không phải lỗi model.
Cold-start smoke được build tuần tự `-m:1 -nodeReuse:false`, rồi chạy DLL trực tiếp.

Kestrel test và moderation service đã được dừng sau kiểm tra. Stack Compose riêng
được dừng bằng `down`, không dùng `down -v`, không xóa/reset database ứng dụng.
File untracked `../nul` được giữ nguyên.
