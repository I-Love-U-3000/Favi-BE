# Cold-start: review, test và tích hợp backend

Ngày 25/10/2026. Phạm vi: backend interests, catalog fallback và chuyển cold/warm.
Không bao gồm frontend onboarding, graph engine thật hoặc moderation branch.

## Review

Đã đọc diff so với main: aggregate/normalization, command/store và concurrency,
mapping EF/version, migration Up/Down/snapshot, query reader/ranker, hai endpoint,
DI, startup seed flag và runner. Không phát hiện lỗi mới chặn tích hợp trong
phạm vi này. Đây là review trong phiên làm việc, chưa có reviewer độc lập.

- Preference lấy user từ JWT; PUT dùng middleware idempotency.
- Version do aggregate tăng một lần; store chuyển concurrency conflict thành 409.
- Host kết nối Auth với ContentDiscovery, không thêm cross-module project reference.
- Query projection/AsNoTracking, lọc public/safe/active và recheck khi hydrate.
- Graph mặc định rỗng, catalog fallback explicit.
- Migration additive cho prerequisite; Down chỉ drop interests, giữ cột legacy.

## Gate local

`tools/cold-start-smoke/verify.ps1`: build, 19/19 unit, 67/67 architecture/privacy,
hai lượt 109/109 checks (9 migration/database + 100 HTTP/status/behavior mỗi lượt).
Mỗi lượt tạo database riêng từ migration cuối main, thêm dữ liệu legacy, Up →
Down → Up rồi registration/JWT/interests/feed/reactions/collection qua HTTP thật.
Hai lượt là hai schema cùng suite, không phải 218 trường hợp độc lập.

Đã kiểm tra concurrent first insert/existing update (một 200/một 409), replay,
version, validation, general/topic preference, cả hai feed, Like/Love/save đầu tiên,
Haha không chuyển warm và gỡ positive cuối trở lại cold. Own-post fixture phân biệt
đường cold/warm thực sự; không mock auth, HTTP hoặc graph response. Legacy field,
Unicode/timestamps/NSFW, Version thiếu và Version có sẵn 9 được giữ đúng.
Rollback giữ legacy rows nhưng mất preferences; re-upgrade tạo bảng interests rỗng.

Script ghi revision, thời gian, từng bước và trạng thái vào `.tools/cold-start-gate.json`;
chỉ exit 0 + passed=true là đạt. Cleanup Compose trong finally. Runner bắt lỗi ở
entrypoint và trả exit 1 để tránh unhandled crash khi dùng CLI. CI chỉ lưu report
và check names, không upload api.log vì runtime log có thông tin nhận dạng JWT.

Workflow `.github/workflows/cold-start.yml` chạy gate trên PR/push main/dispatch;
giới hạn 15 phút, contents:read, giữ evidence 7 ngày. Chưa chạy GitHub CI do không
push trong phiên này. Cần bật required check `backend-gate` trong branch protection;
workflow không tự cấu hình bảo vệ branch. Local pass chưa chứng minh CI Linux pass.

## Tích hợp và rollback

Commit runner/docs/workflow trên feature, squash feature vào main local thành một
commit cold-start để rollback bằng `git revert <cold-start-slice-commit>`. Chạy lại
gate trên main sau merge. Không push/deploy trong phiên này.

Rollback production ưu tiên revert code và giữ schema additive. Nếu chạy Down
phải backup preferences trước; Up không phục hồi chúng. Chỉ dùng database/Redis
trong Compose smoke riêng, không chạy migration/seed vào database ứng dụng.
File `nul` có sẵn được giữ nguyên.

## Điều kiện trước phát hành production

Merge local không phải phê duyệt rollout. Các điều kiện còn lại:

1. Xử lý/đánh giá NU1903 SignalR.Common 9.0.11 có sẵn trên main. Không đổi package
   toàn ứng dụng trong slice này. Warning EF Core Relational 9.0.1/9.0.9 ở tests
   cũng còn; không thay baseline dependency.
2. Gate 3 của tài liệu testing root: k6 qua NGINX nhiều replica, p95 <250ms,
   error rate <0.1% trên workload đại diện. Chưa chạy gate này.
3. Frontend onboarding/gateway E2E staging, backup và rollout trên dữ liệu đại diện.
   Fixture profile/post nhỏ chưa chứng minh hiệu năng hay mọi loại schema drift.
4. Peer review và GitHub required checks trước cập nhật remote main theo quy trình
   nhóm. Moderation pending publication vẫn ngoài scope và chưa được giải quyết.
