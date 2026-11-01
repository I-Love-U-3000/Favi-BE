# Kiểm chứng tích hợp OASIS adapter → Favi — 31/10/2026

Sau lượt fixture được ghi dưới đây, đã bổ sung lượt dùng OASIS engine thật
ManualAction sinh SQLite: 12 tests + 48 checks pass. Chưa LLMAction. Xem
[báo cáo engine/trace/FK upstream](OASIS_SIMULATOR_VERIFICATION.md). Các giới hạn
"chưa simulator" phía dưới là tại thời điểm lượt fixture, không còn mô tả mới nhất.

## Kết quả

Đã pass **12/12 Python adapter tests + 48/48 PostgreSQL/import/HTTP checks**.
Runner exit 0, report tổng `passed=true`. Source adapter có Git blob
`7080ef4e07d1a804cedeffb6c92c5a50180d5c3f`, giống hệt `feat/oasis-adapter:b325c8e`.
Không sửa implementation adapter hoặc API production trong lượt này.

Nhánh làm việc `codex/oasis-favi-integration` được tạo từ `origin/main` tại
`ab875e9` (đã có cold-start), đưa adapter/tooling sang để chạy cùng schema hiện tại.
Chưa merge/push nhánh tích hợp; thay đổi harness/tài liệu đang ở working tree.

Source **fixture SQLite tổng hợp**, chưa phải output từ simulator OASIS/LLM thật.
Fixture cố định ba profiles, ba posts (hai safe/một NSFW), ba likes, một follow,
một comment. Đây là fixture chức năng nhỏ, không đại diện phân phối hoặc tải thật.
Adapter giữ nguyên dữ liệu nguồn, không tự tạo tương tác/skew trong export.

## Đã kiểm chứng

1. Adapter đọc SQLite, xuất snapshot/SQL/CSV và không đổi SHA-256 source.
2. Tự tính lại checksum từng file trong manifest; graph like edges khớp GUID
   của likes vào safe posts. NSFW post không thành positive graph edge.
3. Apply migrations thật vào database riêng rồi execute nguyên file SQL bằng
   Npgsql/PostgreSQL; không sửa cú pháp để chạy bằng SQLite stand-in.
4. Import lại cùng file: JSON tất cả field của sáu bảng import giống hệt,
   không nhân đôi hoặc ghi đè dữ liệu.
5. Database rollback riêng có sentinel profile từ trước. Chèn lỗi FK trước
   COMMIT của SQL, PostgreSQL trả 23503; toàn bộ imported profiles/posts/media/
   reactions/follows/comments rollback, sentinel giữ nguyên.
6. Đối chiếu GUID/author/caption/UTC/NSFW/media URL, like endpoints/type/time,
   follow direction, comment identity/author/post và Unicode/SQL-like literals.
7. Kestrel thật: đăng ký viewer lấy JWT, GET profile/post, media metadata,
   reaction summary, comments, followers và cả hai endpoint feed.
8. `/api/posts/feed` và `/api/posts/feed-with-reposts` chứa đúng hai GUID safe
   từ snapshot; bài NSFW vẫn có trong PostgreSQL nhưng không xuất hiện ở feed.

48 checks gồm 30 checks persistence/import và 18 HTTP/status/behavior checks.
Python fixture assertions và 12 unit tests được báo riêng, không cộng số lặp.

## Môi trường và evidence

- PostgreSQL 15 container: `127.0.0.1:15449`; Redis 7: `127.0.0.1:16399`.
- Kestrel test: `http://127.0.0.1:18191`.
- Compose project: `favi-oasis-smoke`, không dùng project/container ứng dụng.
- Database import: `oasis_import_3035b5f1b2d44507abf72ab3142769a9`.
- Database rollback: `oasis_rollback_f6808b919d354b5291e5d311182bc41d`.
- Report tổng: `.tools/oasis-smoke/latest.json`.
- Snapshot, SQL, SQLite, checks và log của lượt pass:
  `.tools/oasis-smoke/617640fcfa4f402ead61bc6dc945f2e4/`.
- API tự dừng trong finally, Compose được down sau kiểm tra.
- Không chạy migration/seed vào database ứng dụng; giữ nguyên file `../nul`.

Lệnh tái lập từ thư mục solution:

```powershell
./tools/oasis-smoke/verify.ps1
```

Cần Docker Desktop engine hoạt động, .NET SDK 10/runtime 9 và Python 3.10+.
Đường dẫn Python mặc định dùng runtime local có sẵn, có thể truyền `-Python`.
Xem [README runner](../tools/oasis-smoke/README.md).

## Vấn đề đã xử lý trong lúc chạy

Docker Desktop ban đầu không chạy, sau đó crash vì các socket IPC 0 byte không
truy cập được (`dockerInference` và `engine.sock`). Đã dừng các process Docker test,
giữ bản dự phòng các thư mục runtime chứa socket, tạo runtime directory mới rồi
khởi động lại. Không reset factory defaults, không xóa volumes/settings hoặc đọc
secrets. Các thư mục `.backup-<GUID>` được giữ tại vị trí runtime cũ; runner không
tự thực hiện thao tác phục hồi này. Đây là xử lý môi trường local, không là thay
đổi kiến trúc ứng dụng hoặc yêu cầu cài đặt bình thường của adapter.

Một lượt runner fail vì test dùng DateTime local để so JSON timestamp có offset
với PostgreSQL UTC. Đã sửa **test** dùng DateTimeOffset.UtcDateTime và tách assertions;
không đổi timestamp hoặc SQL do adapter sinh ra. Lượt pass cuối xác nhận dữ liệu đúng.
Cảnh báo baseline NU1903 SignalR.Common vẫn tồn tại, không được xử lý trong scope này.

## Chưa kiểm chứng

- Chạy OASIS/LLM thật, schema/data simulator thực tế và hành vi agent.
- Media bytes, static asset serving qua NGINX hoặc hiển thị ảnh ở frontend.
- Luồng moderation/vector indexing; import offline không phát outbox/notifications.
- Tải lớn, RAM/latency, đồng thời nhiều importer hoặc temporal graph evaluation.
- GitHub CI và review độc lập cho các thay đổi tích hợp mới.

Kết luận giới hạn: adapter hiện tại tích hợp được với persistence và read API/feed
của main có cold-start trên fixture đã nêu; chưa đủ bằng chứng cho toàn bộ simulator
đến UI/production hoặc thuật toán recommendation thật.
