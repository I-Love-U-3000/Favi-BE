# Gate integrity cho export trước import — 08/11/2026

Rà soát `verify.ps1`, `Program.cs`, adapter, fixture và simulator xác định khác
biệt thực tế: hai đường sinh mới kiểm checksum sau export; đường
`ExistingRunDirectory` chỉ copy export. C# đọc SQL/snapshot mà không đọc manifest.
Các kiểm entity sau import không thay thế kiểm byte trước thực thi SQL, và không
kiểm CSV. Đây là khoảng hở trong gate tái sử dụng, không phải bằng chứng sáu
dataset gốc đã bị sửa hoặc kết quả benchmark sai.

`ExportIntegrity.Read` nay yêu cầu manifest adapter schema 1 với đủ sáu entry
duy nhất: snapshot.json, favi-seed.sql, users.csv, posts.csv,
user_post_edges.csv, user_user_edges.csv. Tên ngoài danh sách bị từ chối trước đọc
file; mọi SHA256 được so trên raw bytes. Snapshot còn đối chiếu logical payload
hash theo adapter (UTF-8, universal newline normalization). C# dùng đúng payload
đã kiểm. Gate chạy trước kết nối PostgreSQL; `verify.ps1` gọi thêm chế độ
`--verify-export-only` trước Compose, rồi importer kiểm lại trước sử dụng.
Receipt hash source runner nay bao gồm ExportIntegrity.cs.

## Sửa tương thích native projection sau review PR17

Review phát hiện native_projection.export_projection xuất projection-manifest.json,
không phải manifest adapter. CLI cũ trên native-full-10x4-20261101 thực tế thoát1
vì thiếu manifest.json. Gate nay phân biệt hai định dạng: adapter vẫn yêu cầu
sáu file/schema1/logical snapshot hash; native yêu cầu mode chính xác
`explicit Favi subset of a preserved full OASIS world` và đủ hai entry duy nhất
snapshot.json/favi-seed.sql, kiểm SHA256 raw bytes của cả hai. Producer native
không cung cấp logical snapshot hash; không áp đặt hash tự suy diễn lên nó.
Có cả hai manifest thì từ chối vì mơ hồ; manifest adapter hỏng không được fallback
sang native. Hai đường dùng chung kiểm filename, duplicate và payload đã kiểm.
Không tạo CSV giả hoặc sửa export lịch sử để tương thích.

Sau sửa: 23 ExportIntegrityTests PASS (thêm native không CSV, sửa byte/bỏ entry
cả hai file và manifest mơ hồ); OasisSmoke build PASS. CLI chỉ đọc PASS trên
cả native-pilot-20261101 và native-full-10x4-20261101. Logs mới
`.tools/native-integrity-tests.log`, `.tools/native-integrity-build.log`, TRX
`.tools/native-integrity/native-integrity.trx`. Chưa chạy SQL/HTTP/Docker native
trong lần sửa này. PR17 CI của commit a69e137 thành công; kết quả đó chưa kiểm
commit sửa tương thích. Copilot không review được vì hết quota; Codex có finding
native tương thích nêu trên, không được mô tả PR17 là không có findings.

## Bằng chứng

- 17 ExportIntegrityTests PASS: LF và Windows CRLF hợp lệ; sửa byte từng file
  trong sáu file; bỏ từng entry trong sáu entry; logical hash sai; traversal
  entry; duplicate entry. Unit project link trực tiếp helper thật.
- Build OasisSmoke PASS, không lỗi. Cảnh báo NU1903 SignalR và xung đột phiên bản
  EF Core trong unit build đã tồn tại, không được giải quyết trong mốc này.
- CLI thật `OasisSmoke.dll <export> --verify-export-only` PASS trên cả sáu
  confirmatory main/sparse seeds 20261111, 20261112, 20261113, chỉ đọc.
- Copy export main20261111 sang thư mục test riêng rồi thêm comment vào SQL:
  CLI từ chối `Export checksum mismatch: favi-seed.sql`.
- `verify.ps1 -ExistingRunDirectory .tools/import-integrity -SkipToolTests`
  trên bản sao đó từ chối cùng lý do; run.json ghi passed=false,
  composeAttempted=false. Run `.tools/oasis-smoke/5a3e462052da4c66a0c0f480936bbc00`.
- PR16 backend run42452826449 SUCCESS, không review threads tại lúc kiểm.

Raw logs/receipts ignored: `.tools/import-integrity-tests.log`,
`.tools/import-integrity-build.log`, `.tools/import-integrity/export-integrity.trx`,
`.tools/import-integrity/actual-export-checks.json`, rejected-runner.log/json.

Reproduce unit gate từ solution:

```powershell
dotnet test Favi-BE.UnitTests/Favi-BE.UnitTests.csproj --filter FullyQualifiedName~ExportIntegrityTests -m:1 -nodeReuse:false
dotnet build tools/oasis-smoke/OasisSmoke.csproj -m:1 -nodeReuse:false
dotnet tools/oasis-smoke/bin/Debug/net10.0/OasisSmoke.dll .tools/datasets/adaptive-confirmatory-main-20261111/export --verify-export-only
```

Không chạy SQL, API, Docker, fit hoặc holdout trong mốc này. Không sửa dữ liệu,
frozen sources, archives, artifact hoặc kết quả nghiên cứu. Manifest do người
cung cấp kiểm soát: checksum không chứng minh authenticity hoặc chống sửa đồng
bộ. Không dùng gate này để claim SQL parity hay release certification. Các kiểm
import/replay/entity/HTTP hiện hữu giữ nguyên và phải chạy khi cần bằng chứng đó.
