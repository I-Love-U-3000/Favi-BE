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

### 2.3 Cơ Chế Khóa Phân Tán Khi Khởi Động Đa Bản Sao (Cluster Startup Advisory Lock)
Trong môi trường Docker Compose gồm nhiều bản sao API (`favi-api-1`, `favi-api-2`), khi thực hiện cold-start hoặc xóa volume (`docker-compose down -v` rồi `up -d`), cả 2 container sẽ đồng thời gọi `ApplyDatabaseMigrationsAndSeedAsync()`.

Để triệt tiêu hoàn toàn race condition và lỗi xung đột khóa ngoại/trùng lặp bản ghi:
- Hệ thống áp dụng **PostgreSQL Session-Level Advisory Lock** (`SELECT pg_advisory_lock(742189);`) tại [`StartupTasksExtensions.cs`](file:///c:/Users/MINH%20QUANG/Favi/Favi-BE/Favi-BE/Favi-BE.API/DependencyInjection/StartupTasksExtensions.cs).
- Container nào kết nối trước sẽ nắm giữ khóa độc quyền, thực thi toàn bộ chuỗi migration (bao gồm [`20260423000000_AddUpdatedAtAndVersionToProfile`](file:///c:/Users/MINH%20QUANG/Favi/Favi-BE/Favi-BE/Favi-BE.API/Migrations/20260423000000_AddUpdatedAtAndVersionToProfile.cs) bổ sung `UpdatedAt` và `Version` cho bảng `Profiles`) và chạy toàn bộ Seed Pipeline.
- Container thứ hai bị chặn an toàn tại lệnh khóa; khi được giải phóng (`SELECT pg_advisory_unlock(742189);`), nó kiểm tra thấy schema đã mới nhất và seed data đã hiện diện (`hasExistingProfiles == true`), tự động bỏ qua seed và bắt đầu nhận tải.

---

## 3. Cơ Chế Khóa Lạc Quan & Kiểm Soát Phiên Bản Bản Ghi (Optimistic Concurrency Control)

Để ngăn chặn hiện tượng mất dữ liệu do ghi đè đồng thời (**Lost Update Problem**) khi nhiều người dùng hoặc tiến trình cùng chỉnh sửa một tài nguyên (ví dụ: cập nhật thông tin cá nhân, chỉnh sửa bài viết, sửa thông tin bộ sưu tập):

### 3.1 Quy Chuẩn Cột Phiên Bản Trên Database
Các bảng thực thể có khả năng bị sửa đổi đồng thời (`Profiles`, `Posts`, `Collections`, `Comments`, `Stories`, `Messages`, `Conversations`, `Reposts`) được chuẩn hóa với 2 trường kỹ thuật:
- **`Version` (`integer NOT NULL DEFAULT 1`)**: Lưu số hiệu phiên bản hiện tại của bản ghi.
- **`UpdatedAt` (`timestamp with time zone NOT NULL DEFAULT NOW()`)**: Lưu mốc thời gian bản ghi được sửa đổi lần cuối.

### 3.2 Luồng Kiểm Soát Phiên Bản Ở Tầng Ứng Dụng (Application Layer)
Trong kiến trúc CQRS của Favi-BE, cơ chế kiểm soát phiên bản hoạt động khép kín theo luồng:

```
[ Client ]                           [ Command Handler ]                   [ Database ]
    │                                         │                                  │
    │── 1. Query Data (kèm Version = N) ─────▶│                                  │
    │◀─ Trả về Dto (Version = N) ─────────────│                                  │
    │                                         │                                  │
    │── 2. Gửi UpdateCommand(Version = N) ───▶│                                  │
    │                                         │── 3. FindByIdAsync() ───────────▶│
    │                                         │◀─ Entity(Version = M) ───────────│
    │                                         │                                  │
    │                                         ├── M != N? ──▶ Trả 409 Conflict   │
    │                                         │               (CONCURRENCY_      │
    │                                         │                CONFLICT)         │
    │                                         │                                  │
    │                                         └── M == N? ──▶ Tăng Version = N+1 │
    │                                                         UpdatedAt = Now()  │
    │                                                         Save to DB ───────▶│
    │◀─ 4. Phản hồi thành công (200 OK) ──────┴──────────────────────────────────┘
```

1. **Truy vấn**: Client đọc dữ liệu từ các Query Readers, nhận về DTO chứa trường `Version` (ví dụ `Version = 1`).
2. **Gửi lệnh cập nhật**: Khi gửi Command (ví dụ [`UpdateProfileCommand`](file:///c:/Users/MINH%20QUANG/Favi/Favi-BE/Favi-BE/Favi-BE.Modules.Auth/Application/Commands/UpdateProfile/UpdateProfileCommandHandler.cs), `UpdatePostCommand`, `UpdateCollectionCommand`), client bắt buộc phải gửi kèm giá trị `Version` hiện tại mà nó đang giữ.
3. **Đối soát phiên bản**:
   ```csharp
   if (request.Version.HasValue && request.Version.Value != entity.Version)
   {
       return CommandResult.Fail("CONCURRENCY_CONFLICT", 
           "Dữ liệu đã được chỉnh sửa ở phiên làm việc khác. Vui lòng tải lại dữ liệu mới nhất.");
   }
   ```
4. **Cập nhật & Tăng phiên bản**: Nếu phiên bản trùng khớp, handler cập nhật dữ liệu nghiệp vụ, đồng thời tự động tăng `Version = Version + 1` và cập nhật `UpdatedAt = DateTime.UtcNow`.
5. **Mã lỗi phản hồi (HTTP Status)**: Nếu phát hiện xung đột, tầng Controller ánh xạ mã lỗi `CONCURRENCY_CONFLICT` thành **`409 Conflict`**, thông báo cho giao diện người dùng yêu cầu làm mới dữ liệu để tránh ghi đè dữ liệu của người khác.

### 3.3 Lộ Trình Tích Hợp EF Core Concurrency Token (Database-Level)
Hiện tại hệ thống kiểm tra logic phiên bản ở tầng Application Handler. Khi hoàn thiện các migration mapping sâu vào EF Core DbContext, hệ thống chuẩn hóa thêm cấu hình Concurrency Token ở tầng ORM:
```csharp
builder.Entity<Profile>()
    .Property(p => p.Version)
    .IsConcurrencyToken();
```
Nhờ đó, câu lệnh SQL UPDATE do EF Core phát sinh sẽ chứa điều kiện `WHERE "Id" = @id AND "Version" = @oldVersion`. Nếu bản ghi đã bị sửa đổi ở luồng khác, EF Core sẽ ném ngoại lệ `DbUpdateConcurrencyException`, tăng thêm một lớp bảo vệ toàn vẹn dữ liệu ngay cả khi có truy vấn trực tiếp từ ngoài ứng dụng.

---

## 4. Kiến Trúc Seed Pipeline & Dữ Liệu Kiểm Thử

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

### 4.1 Mô Hình Phân Phối Dữ Liệu Thực Tế (90-9-1 Rule & Zipfian Distribution)
Không tạo dữ liệu ngẫu nhiên đồng đều (Uniform distribution), vì trong thực tế:
- 1% người dùng (KOL/Influencer) sở hữu 90% số lượng follower và tương tác.
- 9% người dùng hoạt động thường xuyên.
- 90% người dùng chủ yếu là người xem (lurkers).
Seed pipeline mô phỏng chính xác hiện tượng "Hot User / Hot Post" để kiểm thử hiện tượng tranh chấp khóa (Row Locking) và nghẽn mạng trên database.

### 4.2 Cổng Kiểm Định Bắt Buộc (Mandatory Validation Gate)
Trước khi kết thúc quá trình seed dữ liệu, một bộ validator chạy kiểm tra:
- Không có bất kỳ bản ghi mồ côi (Orphan records) nào bị lỗi Foreign Key.
- Tất cả bài viết gắn với ít nhất một file media hợp lệ.
- Số lượng followers/following trên bảng `profiles` khớp chính xác 100% với số dòng trong bảng `follows`.
