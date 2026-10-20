# 03 — Cơ Chế Idempotency & Độ Tin Cậy Dữ Liệu (Favi-BE)

## 1. Tổng Quan Kiến Trúc Idempotency 2 Lớp

Trong một mạng xã hội chịu tải cao, người dùng thường click đúp (double-click), mạng chập chờn gửi lại request (network retry), hoặc hàng đợi gửi lặp message (at-least-once delivery). Favi-BE bảo vệ toàn vẹn dữ liệu bằng **Kiến trúc Idempotency 2 lớp**:

1. **Lớp Đồng bộ (HTTP API Gateway / Middleware Level)**: Bảo vệ tất cả mutating endpoints chống trùng lặp request từ Client.
2. **Lớp Bất đồng bộ (Messaging / Consumer Level)**: Bảo vệ Outbox/Inbox processor chống trùng lặp xử lý sự kiện tích hợp.

```
[ Lớp 1: HTTP Request ]
Client ──▶ Header: Idempotency-Key ──▶ IdempotencyMiddleware (Redis Lock / Cache)
                                               │
                                      (Nếu mới & hợp lệ)
                                               ▼
                                      Command Handler & SaveChanges
                                               │ (Cùng DB Transaction)
                                      ┌────────┴────────┐
                                      ▼                 ▼
                                [ Data State ]   [ Outbox Event ]
                                                        │
[ Lớp 2: Event Processing ]                             ▼
OutboxProcessor ──▶ Inbox Pattern ──▶ IInbox.TryStartProcessing(msgId, consumer)
                                               │
                                      (Nếu chưa xử lý)
                                               ▼
                                  Notification / Side Effect
```

---

## 2. Chi Tiết Lớp 1: HTTP API `IdempotencyMiddleware`

Được triển khai tại `Favi-BE.API/Middleware/IdempotencyMiddleware.cs`.

### 2.1 Quy Tắc Bắt Buộc Header
- **Áp dụng**: Tất cả các HTTP verbs thay đổi trạng thái: `POST`, `PUT`, `PATCH`, `DELETE` có đường dẫn bắt đầu bằng `/api/`.
- **Header bắt buộc**: `Idempotency-Key` (chuỗi định danh ngẫu nhiên do Client sinh ra, ví dụ UUIDv4, tối đa 128 ký tự).
- **Trường hợp loại trừ (Excluded Prefixes)**:
  - Endpoints xác thực: `/api/auth/login`, `/api/auth/register`, `/api/auth/refresh-token`, `/api/auth/forgot-password`, `/api/auth/reset-password`.
  - Health checks & Docs: `/health`, `/scalar`, `/openapi`.
  - Realtime Hubs: `/chatHub`, `/notificationHub`, `/callHub`.
  - Batch operations / search: `/api/search`, `/api/posts/batch`, `/api/seed`.
  - File upload dạng Multipart (`context.Request.HasFormContentType = true`).

### 2.2 Mã Lỗi Phản Hồi
- Nếu thiếu header `Idempotency-Key`: Trả về `400 Bad Request` với mã lỗi `MISSING_IDEMPOTENCY_KEY`.
- Nếu key dài hơn 128 ký tự: Trả về `400 Bad Request` với mã lỗi `INVALID_IDEMPOTENCY_KEY`.

### 2.3 Cơ Chế Khóa Phân Tán (Redis Distributed Lock)
1. Tạo Redis Cache Key theo format:
   ```
   favi:idempotency:{userId}:{idempotencyKey}
   ```
   *(Với người dùng chưa đăng nhập, `userId` là `"anon"`)*.

2. **Chống Concurrency Race Condition**:
   - Khi request đến, middleware thực hiện:
     ```csharp
     var acquired = await db.StringSetAsync(cacheKey, "IN_FLIGHT", TimeSpan.FromSeconds(60), When.NotExists);
     ```
   - Nếu `acquired == false` (tức là request cùng key đang được một luồng khác xử lý):
     Hệ thống ngay lập tức trả về **`409 Conflict`** với payload:
     ```json
     {
       "statusCode": 409,
       "code": "IDEMPOTENT_OPERATION_IN_PROGRESS",
       "message": "Yêu cầu trước đó đang được xử lý, vui lòng chờ."
     }
     ```

3. **Lưu Kết Quả Phản Hồi (Response Caching)**:
   - Nếu handler thực thi thành công trả về HTTP Status `2xx`:
     Hệ thống serialize `(StatusCode, ContentType, ResponseBody)` và lưu đè vào Redis key đó với **TTL = 5 phút** (`TimeSpan.FromMinutes(5)`).
   - Khi Client gửi lại đúng `Idempotency-Key` trong vòng 5 phút:
     Middleware đọc cache từ Redis và phát lại y hệt response cũ mà **không thực thi lại Handler hay chèn thêm dữ liệu vào Database**.

4. **Tự Động Mở Khóa Khi Lỗi (Rollback on Failure)**:
   - Nếu handler trả về status code không phải `2xx` (ví dụ `400`, `500`) hoặc ném ngoại lệ không xử lý:
     Middleware ngay lập tức xóa key `favi:idempotency:...` khỏi Redis (`await db.KeyDeleteAsync(cacheKey)`).
   - Mục đích: Cho phép người dùng hoặc client sửa thông tin và retry lại ngay lập tức mà không bị kẹt lỗi 409.

---

## 3. Chi Tiết Lớp 2: Messaging & Consumer Idempotency (Outbox + Inbox)

### 3.1 Vấn Đề Gốc
Trước đây, các hàm như `PostService.ToggleReactionAsync` gọi trực tiếp `INotificationService` trong transaction. Nếu notification lỗi, không có retry; nếu SignalR push xong mà DB commit thất bại thì sinh ra thông báo ma (ghost notification).

### 3.2 Giải Pháp Outbox Nguyên Tử
Mọi sự kiện tích hợp (Integration Event) đều được lưu vào bảng `OutboxMessages` **trong cùng 1 DbContext SaveChanges / Database Transaction** với dữ liệu chính:

```csharp
// Trong Command Handler
_postRepository.Add(reaction);
await _outbox.AddAsync(new PostReactionToggledIntegrationEvent(postId, userId), cancellationToken);
await _unitOfWork.SaveChangesAsync(cancellationToken); // COMMIT CÙNG NHAU
```

### 3.3 Đảm Bảo Idempotency Tại Consumer với Bảng Inbox
`OutboxProcessor` định kỳ đọc các message chưa gửi và dispatch đến consumer. Vì cơ chế network có thể retry dẫn đến duplicate delivery, mỗi consumer bắt buộc sử dụng `IInbox`:

```csharp
public async Task Handle(PostReactionToggledIntegrationEvent notification, CancellationToken ct)
{
    var consumerName = nameof(PostReactionToggledNotificationConsumer);
    
    // 1. Kiểm tra idempotency gate: Khóa đơn vị (MessageId, ConsumerName)
    var started = await _inbox.TryStartProcessingAsync(notification.Id, consumerName, ct);
    if (!started)
    {
        // Đã được xử lý hoặc đang được consumer này xử lý -> bỏ qua an toàn
        return;
    }

    try
    {
        // 2. Thực thi side-effect (Tạo thông báo, đẩy SignalR)
        await _notificationRepository.AddAsync(...);
        await _realtimeGateway.PushNotificationAsync(...);

        // 3. Đánh dấu đã xử lý thành công
        await _inbox.MarkProcessedAsync(notification.Id, consumerName, ct);
    }
    catch (Exception ex)
    {
        // Khi lỗi, Inbox cho phép retry ở chu kỳ poll kế tiếp
        await _inbox.MarkFailedAsync(notification.Id, consumerName, ex.Message, ct);
        throw;
    }
}
```

**Nguyên lý bất biến**: Cặp khóa `(messageId, consumerName)` là đơn vị idempotency duy nhất. Một message có thể gửi tới 5 consumers khác nhau, mỗi consumer tự bảo vệ trạng thái của riêng mình mà không bị ảnh hưởng chéo.
