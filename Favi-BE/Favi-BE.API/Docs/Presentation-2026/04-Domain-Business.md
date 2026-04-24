# 04 — Domain & Nghiệp Vụ (Domain-Driven Design)

---

## 1. Tại sao chọn Domain-Driven Design (DDD)?

Trước đây, hệ thống phát triển theo hướng **Data-Driven (Database-Centric)**:
- Logic nằm rải rác ở Controller, Service, thậm chí dưới database.
- Các thực thể (Entities) chỉ là các túi chứa dữ liệu (Anemic Domain Model) với các public setter (`public string Name { get; set; }`), ai cũng có thể thay đổi state từ bất kỳ đâu.
- Khi business rule thay đổi, phải truy tìm và sửa logic ở nhiều nơi.

**Giải pháp với DDD:**
- **Đưa business logic vào trọng tâm**: Code phải phản ánh chính xác ngôn ngữ và quy tắc nghiệp vụ.
- **Rich Domain Model**: Entity tự bảo vệ trạng thái của nó. Thay vì `profile.IsBanned = true`, ta dùng `profile.Ban(reason, until)`.
- **Align Business & Tech**: Bounded Contexts phân chia ranh giới rõ ràng, giúp team kỹ thuật hiểu và nói cùng ngôn ngữ với team nghiệp vụ (Ubiquitous Language).

---

## 2. Bounded Contexts & Module Mapping

Dựa vào phân tích nghiệp vụ của Favi, hệ thống được chia thành 8 Bounded Contexts chính, tương ứng với các Module trong kiến trúc Modular Monolith:

| Bounded Context | Tên Module | Trách nhiệm cốt lõi |
|-----------------|------------|---------------------|
| **Identity & Access** | `Auth` | Xác thực, phân quyền, quản lý thông tin hồ sơ (Profile) định danh. |
| **Social Graph** | `SocialGraph` | Mối quan hệ giữa người với người (Follow, Unfollow, Social Links). |
| **Content Publishing** | `Content` | Vòng đời của bài viết, bộ sưu tập (Collection), hệ thống Tag. |
| **Engagement** | `Engagement` | Tương tác của người dùng với nội dung (Comment, Reaction). |
| **Notifications** | `Notifications` | Quản lý và phân phối thông báo đến người dùng. |
| **Stories** | `Stories` | Nội dung ngắn hạn (24h), quản lý lượt xem (Views). |
| **Messaging** | `Messaging` | Trò chuyện trực tiếp (Direct Message), nhóm chat. |
| **Moderation & Trust** | `Moderation` | Báo cáo vi phạm (Report), cấm tài khoản, quản lý Admin Actions. |

---

## 3. Aggregates & Invariants (Bất biến)

**Aggregate Root** là "cửa ngõ" duy nhất để thay đổi dữ liệu bên trong nó. Mọi thay đổi phải tuân thủ các quy tắc nghiệp vụ (Invariants).

### Ví dụ 1: Module Identity & Access
- **Aggregate `Profile`**: Quản lý thông tin định danh và cài đặt quyền riêng tư.
  - *Invariant*: Username phải hợp lệ và duy nhất (mức domain policy). Không thể thay đổi trạng thái "Banned" mà không có lý do hoặc thời hạn.
- **Aggregate `EmailAccount`**: Quản lý thông tin đăng nhập.
  - *Invariant*: Email phải hợp lệ, Password luôn phải được hash trước khi lưu.

### Ví dụ 2: Module Engagement
- **Aggregate `CommentThread`**: Quản lý một cụm bình luận và các replies.
  - *Invariant*: Comment con (reply) phải thuộc về cùng một bài viết với Comment cha. Không cho phép reply một comment đã bị xóa.
- **Aggregate `Reaction`**: Quản lý lượt thích.
  - *Invariant*: Một user chỉ được thả 1 reaction trên 1 target (Post/Comment). Thả lần 2 là Toggle (xóa).

### Việc thực thi Invariant bằng IBusinessRule
Trong Shared Kernel (`BuildingBlocks`), chúng ta định nghĩa interface `IBusinessRule`.
```csharp
protected static void CheckRule(IBusinessRule rule)
{
    if (rule.IsBroken())
        throw new BusinessRuleValidationException(rule.Message);
}
```
Entity trước khi thay đổi trạng thái sẽ gọi `CheckRule()`. Nếu vi phạm, nó ném lỗi ngay tại tầng Domain, không cần đợi đến lúc lưu xuống Database.

---

## 4. Giao tiếp giữa các Bounded Contexts

Trong DDD, các Bounded Context không được chọc trực tiếp vào database của nhau. Sự giao tiếp được thực hiện qua **Integration Events**.

**Ví dụ thực tế đã triển khai:**
1. **Engagement Context**: User A thả tim bài viết của User B.
2. Aggregate `Reaction` gọi `AddDomainEvent(new PostReactionToggledEvent(...))`.
3. Event này được chuyển hóa thành Outbox Message.
4. **Notifications Context**: Lắng nghe Outbox Message, tạo bản ghi Notification và push SignalR.
5. **Engagement Context** hoàn toàn không biết sự tồn tại của **Notifications Context**. Ranh giới (Boundary) được bảo vệ tuyệt đối.

---

## 5. Ubiquitous Language (Ngôn ngữ chung)

Toàn bộ naming convention trong codebase phải phản ánh đúng từ vựng nghiệp vụ.
- Không dùng: `CreateUser()`, `UpdateProfileStatus()`.
- Dùng: `RegisterAccount()`, `BanUser()`, `RevokeBan()`.
- Không dùng: `ChangePasswordHash`.
- Dùng: `ChangePasswordCommand`.

Điều này giúp tài liệu (như bảng Inventory) khớp 1:1 với tên Class và Handler trong code, giảm thiểu chi phí dịch thuật giữa "Business nói" và "Dev hiểu".
