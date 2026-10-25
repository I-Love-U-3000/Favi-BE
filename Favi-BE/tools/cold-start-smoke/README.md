# Cold-start HTTP smoke test

## Gate backend trước merge

Từ thư mục solution chạy `./tools/cold-start-smoke/verify.ps1` bằng PowerShell
ngoài sandbox. Script fail ngay khi build/test/check thất bại, chạy unit và
architecture tests, hai schema upgrade/rollback/re-upgrade + HTTP E2E, dừng stack
riêng trong finally và ghi `.tools/cold-start-gate.json`. Chỉ `passed=true` cùng
exit 0 mới được tính đạt. Script không commit, merge, push hay deploy.

Suite kiểm tra cả concurrent UPDATE trên row có sẵn, Haha không chuyển warm,
gỡ Like/Love/save cuối cùng trở lại cold. Rollback được chạy trước khi khởi động
API test: lưu preference, Down về migration cuối main, xác nhận dữ liệu legacy
còn nguyên và bảng interests bị drop; Up lại phải tạo bảng rỗng. **Down làm mất
preferences**, không phải rollback không mất dữ liệu. Production ưu tiên rollback
code, giữ schema additive; cần backup preferences nếu thực sự rollback schema.
Các cột/index prerequisite được giữ lại theo migration Down.

Đây là gate backend qua Kestrel/PostgreSQL/Redis thật với fixture nhỏ. Không phải
chứng nhận production rollout, frontend E2E hoặc Gate 3 k6/NGINX nhiều replica
trong `document/07_TESTING_AND_BENCHMARKING.md` ở root repo. Những gate đó cần
chạy trên môi trường staging riêng trước phát hành production.

## Kiểm chứng trước merge: nâng cấp và cold → warm

Chạy bằng CLI ngoài sandbox nếu sandbox chặn loopback hoặc tiến trình .NET:

```powershell
docker compose -f tools/cold-start-smoke/compose.yml up -d --wait
dotnet build tools/cold-start-smoke/ColdStartSmoke.csproj -m:1 -nodeReuse:false
dotnet tools/cold-start-smoke/bin/Debug/net10.0/ColdStartSmoke.dll --upgrade
dotnet tools/cold-start-smoke/bin/Debug/net10.0/ColdStartSmoke.dll --upgrade --existing-columns
```

`--upgrade` tạo database test mới `favi_cold_upgrade_<GUID>` trên cùng PostgreSQL
test 15439, migrate tới `20260423000000_AddUpdatedAtAndVersionToProfile` (migration
cuối main), thêm profile/post legacy bằng SQL trước khi nâng cấp. So sánh toàn bộ
field cũ bằng JSON PostgreSQL, kiểm tra Version profile có sẵn, Version post được
backfill, cờ NSFW giữ nguyên, không tạo interests hàng loạt và migrate lần hai không
đổi dữ liệu. Không phải bản sao database ứng dụng thật.

`--existing-columns` thêm trước `Posts.Version=9` và index Posts cùng tên migration
định tạo, rồi kiểm tra nâng cấp vẫn giữ Version=9. Cờ này bắt buộc đi kèm `--upgrade`.
Logs của biến thể này nằm trong `.tools/cold-start-upgrade-existing/`.

Cả chế độ thường và upgrade chạy thêm ba account độc lập cho Like, Love, save.
Mỗi account có own-post fixture: trước positive cả hai feed loại own post; sau
positive đầu tiên qua HTTP, cả hai feed warm phải có own post. Collection được tạo
qua multipart HTTP không upload ảnh. Own post chỉ được tạo trực tiếp trong database
test để làm dấu phân biệt nhánh, không giả response graph hay authentication.
Logs/checks upgrade nằm trong `.tools/cold-start-upgrade/`. Database upgrade được
giữ đến khi dừng stack riêng, không dùng lệnh DROP database ứng dụng.

Chạy API thật bằng Kestrel tại `127.0.0.1:18181`, PostgreSQL 15 tại 15439,
Redis 7 tại 16389. Không dùng EF InMemory, TestServer, fake authentication hoặc
mock graph response. Graph mặc định chưa có nên dùng catalog fallback thật.

Từ thư mục chứa solution, cần Docker Desktop đang chạy và .NET 10 SDK:

```powershell
docker compose -f tools/cold-start-smoke/compose.yml up -d --wait
dotnet run --project tools/cold-start-smoke/ColdStartSmoke.csproj
```

Tool chỉ dùng database `favi_cold_smoke` trong project Compose riêng. Nó apply
migrations thật và thêm fixture nhỏ (ba topic, public/private/NSFW posts), sau đó
khởi động API với `Seeding:Enabled=false`. Cờ này chỉ bỏ seed benchmark lớn,
không bỏ migrations, auth, MediatR, transaction hay idempotency middleware.
Mỗi lần chạy đăng ký tài khoản mới qua HTTP; không xóa dữ liệu database cũ.
Cloudinary client có dummy settings để controller có thể khởi tạo; không upload
ảnh/gọi Cloudinary trong test cold-start.

40 kiểm tra HTTP/status/behavior ban đầu bao gồm:

- Unauthorized 401; catalog 200; đăng ký thật lấy JWT.
- Default interests/version, general đa chủ đề.
- Lọc NSFW/private, một hoặc nhiều chủ đề, reset general.
- PUT thiếu key 400, Redis replay, stale version 409.
- Hai tài khoản độc lập; hai PUT đồng thời chỉ một thành công.
- Cả feed và feed-with-reposts, validation page, phân trang không trùng.

Các lượt trước pass 82 checks cho upgrade + cold → warm. Gate hiện mở rộng thêm
rollback, concurrent UPDATE, non-positive và warm → cold; số checks chính xác
được in khi runner hoàn tất, không dùng số lịch sử để suy ra kết quả lượt mới.

Tool tự dừng API ở finally. Report/log nằm trong `.tools/cold-start-smoke/`;
chỉ đọc report PASS nếu process thoát 0. Không public access token ra console.
Đây là fixture smoke test, không phải load test, không chạy qua NGINX/frontend,
không đánh giá thuật toán recommendation hay graph engine chưa có.

Sau khi kiểm tra, dừng stack riêng (không đụng stack Favi khác):

```powershell
docker compose -f tools/cold-start-smoke/compose.yml down
```

Lỗi version increment kép phát hiện khi chạy test thật đã được sửa: aggregate
ProfileInterests tự tăng version, DbContext không tăng lần thứ hai. Test reader
thật và HTTP nhiều lần cập nhật kiểm tra version 1 → 2 → 3.
