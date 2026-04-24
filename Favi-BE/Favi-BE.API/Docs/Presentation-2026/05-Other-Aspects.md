# 05 — Các khía cạnh kỹ thuật & nghiệp vụ khác

---

## 1. Schema Transition & Data Migration

Khi chuyển đổi từ Monolith cũ sang kiến trúc mới có sử dụng Outbox/Inbox và tách ranh giới module, chiến lược chuyển đổi cấu trúc dữ liệu (Schema Transition) là yếu tố sống còn.

### Vấn đề:
- Hệ thống cũ đang lưu chung tất cả trong 1 database lớn (AppDbContext).
- Kiến trúc mới yêu cầu các module (như Auth, Notifications) chỉ được thao tác với những bảng mà nó sở hữu.

### Giải pháp (Additive Migration):
1. **Không phá vỡ Schema cũ**: Không drop bảng, không xóa cột ngay lập tức.
2. **Thêm bảng mới cho Hạ tầng**: Chạy migration để tạo bảng `OutboxMessages` và `InboxMessages`.
3. **Module-specific DbContexts (Tương lai)**: Hiện tại các Module Port Adapter đang gọi chung vào `AppDbContext`. Ở các phase sau (Phase 4), khi tách database, chúng ta sẽ tạo các DbContext riêng cho từng module, map đúng các bảng thuộc quyền sở hữu của nó.
4. **Data Sync**: Đối với các thực thể liên quan đến cả 2 hệ thống cũ - mới (như việc tách `Profile` thành Identity và Social Graph), ta dùng event để backfill dữ liệu nếu cần.

---

## 2. Chiến lược Kiểm thử (Testing Strategy)

Để đảm bảo việc cấu trúc lại không làm vỡ các tính năng đang chạy, hệ thống kiểm thử được tổ chức theo tháp 3 tầng:

1. **Architecture Tests (Kiểm thử kiến trúc)**:
   - Dùng thư viện `NetArchTest`.
   - **Quy tắc cứng**: Project Domain không được tham chiếu Application; Controllers không được gọi Service cũ trực tiếp (sau khi migrate); Module không được gọi chéo lẫn nhau.
   - Chạy tự động trên CI để chặn ngay các đoạn code vi phạm ranh giới (Boundary Enforcement).

2. **Unit Tests (Kiểm thử đơn vị)**:
   - Focus vào các Domain Entities: Đảm bảo Invariants (IBusinessRule) hoạt động đúng.
   - Focus vào các MediatR Command Handlers: Mock các Adapter/Port để test logic nghiệp vụ thuần túy.

3. **Integration Tests (Kiểm thử tích hợp)**:
   - Chứng minh luồng Outbox/Inbox hoạt động: Dispatch 1 Command -> Check DB xem state đổi chưa -> Check Outbox xem event được sinh ra chưa -> Giả lập OutboxProcessor -> Check Inbox.

---

## 3. Observability & Traceability (Giám sát hệ thống)

Với Event-Driven Architecture, khi một Request bị rẽ nhánh thành các công việc chạy nền (Background Tasks), việc debug sẽ trở thành "ác mộng" nếu thiếu công cụ truy vết.

**Cơ chế Traceability đang áp dụng:**
- **CorrelationId**: Khởi tạo từ Request HTTP đầu vào (`IExecutionContextAccessor`).
- **Lưu trữ**: ID này được đóng gói vào payload của Outbox Message.
- **Log Scopes**: Khi `OutboxProcessor` chạy nền, nó bọc toàn bộ chu trình xử lý bằng `_logger.BeginScope()`, nhúng `CorrelationId` và `OutboxMessageId` vào ngữ cảnh của Logger.
- **Kết quả**: Dù Notification gửi thành công hay thất bại qua SignalR ở Background, ta chỉ cần filter theo `CorrelationId` trên Kibana/Seq để thấy toàn bộ hành trình từ lúc User click "Comment" cho đến lúc bạn bè nhận được thông báo.

---

## 4. Bảo mật & Privacy (Cross-Cutting Concerns)

Bảo mật không nằm riêng rẽ mà được thiết kế xuyên suốt các Module thông qua cơ chế Pipeline:

- **Authentication**: Xác thực người dùng (JWT) được xử lý tập trung ở Middleware. ID của user hiện tại được inject qua `IExecutionContextAccessor`.
- **Authorization**: Được xử lý trực tiếp ở tầng Command Handler hoặc Controller.
- **Privacy Policy**: Hệ thống có `IPrivacyGuard` (tạm nằm ở BuildingBlocks/API) để quyết định xem User A có được xem Post của User B không (dựa trên thiết lập Public/Private/Followers-only). Sau này, logic này sẽ được đẩy sâu vào trong các Domain Entities.

---

## 5. Deployment & CI/CD Pipeline

Kiến trúc Modular Monolith mang lại sự đơn giản tối đa cho việc Deploy:

- **Single Deployment Unit**: Vẫn chỉ là một project Web API duy nhất (`Favi-BE.API`) được build và deploy. Không có độ trễ network, không cần service mesh phức tạp như Microservices.
- **Hosted Services**: OutboxProcessor và InboxProcessor chạy như các BackgroundWorker bên trong chính process của Kestrel. Không tốn thêm tài nguyên server.
- **Rolling Updates**: Nhờ Strangler Fig Pattern, nếu bản deploy có lỗi ở Command Handler mới, chỉ cần revert code hoặc đổi cấu hình DI quay về Service cũ rồi deploy lại (thời gian tính bằng giây), không ảnh hưởng đến database.
