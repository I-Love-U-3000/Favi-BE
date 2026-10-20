# 02 — Cấu Trúc Phân Tầng, MediatR Pipeline & Design Patterns (Favi-BE)

## 1. Cấu Trúc Phân Tầng Trong Từng Module (Onion / Hexagonal)

Mỗi module trong Favi-BE được tổ chức theo mô hình **Onion Architecture** khép kín:

```
┌────────────────────────────────────────────────────────┐
│                      Infrastructure                    │
│   EF Core Configurations, Adapters, Outbox/Inbox IO    │
│  ┌──────────────────────────────────────────────────┐  │
│  │                    Application                   │  │
│  │    Commands, Queries, Handlers, Validators, DTOs │  │
│  │  ┌────────────────────────────────────────────┐  │  │
│  │  │                   Domain                   │  │  │
│  │  │  Aggregates, Entities, Value Objects,      │  │  │
│  │  │  Domain Events, Business Invariants        │  │  │
│  │  └────────────────────────────────────────────┘  │  │
│  └──────────────────────────────────────────────────┘  │
└────────────────────────────────────────────────────────┘
```

### 1.1 Tầng Domain (Domain Layer)
- Không có bất kỳ dependency nào vào Entity Framework, ASP.NET Core hay SQL.
- Chứa các Aggregate Root kế thừa từ `Entity<TId>` và áp dụng các quy tắc bất biến thông qua `CheckRule(new SomeBusinessRule(...))`.
- Mọi biến đổi trạng thái nghiệp vụ bắt buộc phải thông qua method của Aggregate (ví dụ: `post.UpdateCaption(...)`, `reaction.Toggle(...)`), không cho phép setter public tự do.
- Phát sinh Domain Event (`IDomainEvent`) khi trạng thái nghiệp vụ cốt lõi thay đổi.

### 1.2 Tầng Application (Application Layer)
- Triển khai CQRS với MediatR:
  - **Commands**: Kế thừa `ICommand<TResult>` (hoặc `IRequest<TResponse>`). Là các tác vụ làm thay đổi trạng thái hệ thống.
  - **Queries**: Kế thừa `IQuery<TResult>`. Chỉ đọc dữ liệu, không bao giờ mutate.
- Chứa các DTOs (Data Transfer Objects) và ViewModel trả về.
- Xác thực dữ liệu đầu vào sử dụng **FluentValidation** (`AbstractValidator<TRequest>`).
- Định nghĩa các Ports (Interfaces) giao tiếp ra ngoài module (ví dụ: `IAuthWriteRepository`, `ICommentQueryReader`).

### 1.3 Tầng Infrastructure (Infrastructure Layer)
- Triển khai các Ports được định nghĩa ở tầng Application.
- Cấu hình EF Core Fluent API (`IEntityTypeConfiguration<TEntity>`), mapping sang bảng PostgreSQL.
- Quản lý các repository cụ thể, Outbox publisher, và Inbox consumer.

---

## 2. API Host Mỏng (`Favi-BE.API`) & Pipeline Behaviors

Tầng `Favi-BE.API` là host khởi chạy (Composition Root). Controller tại đây cực kỳ mỏng, chỉ nhận HTTP request, chuyển đổi sang MediatR Command/Query và trả về HTTP response:

```csharp
[HttpPost]
public async Task<IActionResult> CreatePost([FromBody] CreatePostRequest request, CancellationToken ct)
{
    var command = new CreatePostCommand(User.GetUserId(), request.Caption, request.MediaUrls, request.Tags);
    var result = await _mediator.Send(command, ct);
    return CreatedAtAction(nameof(GetPostById), new { id = result.Id }, result);
}
```

### 2.1 Chuỗi Pipeline Behaviors của MediatR

Mọi request gửi qua `_mediator.Send()` đều phải đi qua pipeline hành vi có thứ tự nghiêm ngặt:

```
HTTP Request 
     │
     ▼
┌─────────────────────────┐
│   ValidationBehavior    │ ──▶ Chạy FluentValidation; ném ValidationException nếu sai format
└────────────┬────────────┘
             │
             ▼
┌─────────────────────────┐
│     LoggingBehavior     │ ──▶ Ghi log Request name, CorrelationId, UserId, Execution payload
└────────────┬────────────┘
             │
             ▼
┌─────────────────────────┐
│   PerformanceBehavior   │ ──▶ Đo thời gian chạy; ghi warning nếu execution > 500ms
└────────────┬────────────┘
             │
             ▼
┌─────────────────────────┐
│   TransactionBehavior   │ ──▶ (Chỉ áp dụng cho ICommand) Mở EF Transaction, commit nếu OK,
└────────────┬────────────┘     rollback nếu có unhandled exception
             │
             ▼
┌─────────────────────────┐
│     Command / Query     │ ──▶ Business logic thực tế trong Handler
│         Handler         │
└─────────────────────────┘
```

---

## 3. Quản Lý Dependency Injection (DI Composition)

Để tránh hiện tượng `Program.cs` biến thành một "God Class" chứa hàng trăm dòng đăng ký dịch vụ lộn xộn, hệ thống áp dụng quy tắc đóng gói theo module:

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// 1. Hạ tầng cốt lõi
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddAuthentication(builder.Configuration);
builder.Services.AddApplication(builder.Configuration);

// 2. Từng Module độc lập
builder.Services.AddAuthModule();
builder.Services.AddSocialGraphModule();
builder.Services.AddContentPublishingModule();
builder.Services.AddContentDiscoveryModule();
builder.Services.AddEngagementModule();
builder.Services.AddNotificationsModule();
builder.Services.AddStoriesModule();
builder.Services.AddMessagingModule();
builder.Services.AddModerationModule();

var app = builder.Build();
app.UseStartupTasks(); // Chạy migrations & kiểm tra kết nối
```

> **Quy tắc tuyệt đối**: Tuyệt đối không đăng ký các repository hoặc service nội bộ của module trực tiếp trong `Program.cs`. Mỗi module bắt buộc phải có file extension method riêng (ví dụ `AddAuthModule.cs`) để tự đăng ký các dependencies của chính nó.

---

## 4. Mô Hình Strangler Fig Pattern & Rollback Hợp Lệ

Favi-BE áp dụng **Strangler Fig Pattern** để chuyển dịch dần các nghiệp vụ từ monolith cũ sang CQRS module mới mà không làm gián đoạn hệ thống:

```
[ Giai đoạn 1: Triển khai Handler mới song song ]
Controller ──▶ _mediator.Send(Command) ──▶ Modular Handler
                (Legacy IAuthService vẫn được giữ trong DI làm cổng rollback an toàn)

[ Giai đoạn 2: Ký duyệt tương đồng (Parity Signoff) ]
Sau khi automated tests và load tests chứng minh handler mới đạt độ ổn định 100%,
xóa đăng ký DI cho legacy service.

[ Giai đoạn 3: Dọn dẹp (Cleanup) ]
Xóa mã nguồn cũ và các bảng/cột trung gian không còn sử dụng.
```

### Hợp đồng Rollback (Rollback Contract)
- Mỗi slice chuyển dịch được đóng gói thành **1 Git Commit độc lập**.
- Nếu xảy ra sự cố production, hành động rollback chỉ là chạy `git revert <slice-commit-hash>` để khôi phục lại luồng cũ ngay lập tức mà không cần chỉnh sửa tay code phức tạp.
