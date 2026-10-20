# 06 — Quản Lý Phiên Bản (Git), Migrations & Seed Pipeline (Favi-BE)

## 1. Chiến Lược Quản Lý Phiên Bản (Version Control & Git)

Hệ thống Favi-BE áp dụng quy trình kiểm soát phiên bản tiêu chuẩn để đảm bảo mã nguồn luôn ổn định và có khả năng truy vết:

### 1.1 Quy Chuẩn Đặt Tên Commit (Conventional Commits)
Mỗi commit phải chỉ rõ loại thay đổi và phạm vi (scope):
- `feat(auth): add auth session aggregate and query reader`
- `fix(engagement): prevent race condition on comment reaction toggle`
- `refactor(socialgraph): extract follow relations into dedicated module`
- `test(e2e): add k6 scenario for feed scrolling pagination`
- `docs(arch): update bounded contexts dependency matrix`

### 1.2 Hợp Đồng Rollback Nhị Phân (Binary Rollback Contract)
- Khi thực hiện di dời nghiệp vụ theo mô hình Strangler Fig, **mỗi slice hoàn chỉnh phải nằm trong đúng 1 Git Commit**.
- Quy tắc rollback: Trong trường hợp kiểm thử sau deploy phát hiện lỗi hồi quy (regression bug), rollback được thực hiện bằng lệnh duy nhất:
  ```bash
  git revert <slice-commit-hash>
  ```
  Nhờ đó, DI registration và route của module cũ sẽ lập tức hoạt động lại bình thường mà không cần debug vội vã trên production.

---

## 2. Quy Trình Tiến Hóa Cơ Sở Dữ Liệu (EF Core Migrations)

### 2.1 Quy Tắc Không Gây Downtime (Zero-Downtime Schema Evolution)
Khi thay đổi cấu trúc bảng cơ sở dữ liệu trên production:
1. **Thay đổi có tính chất cộng dồn (Additive Only)**:
   - Thêm cột mới nullable (`NULL`) hoặc có giá trị mặc định (`DEFAULT`).
   - Tạo bảng mới.
   - Không được phép đổi tên hoặc xóa cột ngay lập tức khi ứng dụng cũ vẫn đang chạy.
2. **Quy trình 3 bước khi đổi tên hoặc xóa cột**:
   - *Bước 1*: Tạo cột mới, deploy code ghi dữ liệu vào cả cột cũ và mới (Dual-write).
   - *Bước 2*: Chạy script backfill đồng bộ dữ liệu từ cột cũ sang mới; chuyển toàn bộ luồng đọc sang cột mới.
   - *Bước 3*: Sau khi kiểm chứng an toàn, tạo migration xóa cột cũ.

### 2.2 Các Lệnh EF Core Thường Dùng
Thực hiện trong thư mục `Favi-BE/Favi-BE`:
```bash
# Tạo migration mới cho DbContext
dotnet ef migrations add <TenMigration> --project Favi-BE.API --startup-project Favi-BE.API

# Cập nhật database trực tiếp
dotnet ef database update --project Favi-BE.API --startup-project Favi-BE.API

# Xuất file script SQL để DBA review trước khi deploy
dotnet ef migrations script --idempotent --output migration.sql --project Favi-BE.API
```

---

## 3. Kiến Trúc Seed Pipeline & Dữ Liệu Kiểm Thử

Để phục vụ kiểm thử tải cao (load testing), đo lường hiệu năng và kiểm thử chức năng giao diện, hệ thống tích hợp sẵn một **Seed Pipeline Tất định (Deterministic Seed Pipeline)**:

```
[ Step 0: Constants & Context ] ──▶ Đọc config, khởi tạo seed context
              │
              ▼
[ Step 1: Users & Profiles ]    ──▶ Tạo tài khoản mẫu với phân phối avatar thực tế
              │
              ▼
[ Step 2: Social Graph ]        ──▶ Tạo quan hệ Follow theo phân phối Power-Law (Zipf)
              │
              ▼
[ Step 3: Posts & Media ]       ──▶ Chèn bài viết, liên kết ảnh tĩnh từ /seed-assets/
              │
              ▼
[ Step 4: Engagement ]          ──▶ Tạo cây bình luận phân cấp và lượt thích
              │
              ▼
[ Step 5: Tags & Collections ]  ──▶ Gắn hashtags và gom nhóm bộ sưu tập
              │
              ▼
[ Step 6: Notifications ]       ──▶ Sinh thông báo mẫu cho người dùng
              │
              ▼
[ Step 7: Stories ]             ──▶ Tạo stories còn hạn (< 24h) và đã hết hạn
              │
              ▼
[ Mandatory Validation Gate ]   ──▶ Kiểm tra tính toàn vẹn, tỷ lệ dữ liệu & Foreign Keys
```

### 3.1 Mô Hình Phân Phối Dữ Liệu Thực Tế (90-9-1 Rule & Zipfian Distribution)
Không tạo dữ liệu ngẫu nhiên đồng đều (Uniform distribution), vì trong thực tế:
- 1% người dùng (KOL/Influencer) sở hữu 90% số lượng follower và tương tác.
- 9% người dùng hoạt động thường xuyên.
- 90% người dùng chủ yếu là người xem (lurkers).
Seed pipeline mô phỏng chính xác hiện tượng "Hot User / Hot Post" để kiểm thử hiện tượng tranh chấp khóa (Row Locking) và nghẽn mạng trên database.

### 3.2 Cổng Kiểm Định Bắt Buộc (Mandatory Validation Gate)
Trước khi kết thúc quá trình seed dữ liệu, một bộ validator chạy kiểm tra:
- Không có bất kỳ bản ghi mồ côi (Orphan records) nào bị lỗi Foreign Key.
- Tất cả bài viết gắn với ít nhất một file media hợp lệ.
- Số lượng followers/following trên bảng `profiles` khớp chính xác 100% với số dòng trong bảng `follows`.
