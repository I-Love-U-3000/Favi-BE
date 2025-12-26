# Hướng Dẫn Vẽ Domain Model Mới cho Favi

> [!IMPORTANT]
> **Nguồn sự thật (Single Source of Truth)**: Domain Model này được xây dựng dựa trên các tài liệu Use Case chi tiết phiên bản **2.1.x** (`UC_2.1.1` đến `UC_2.1.14` và `USE_CASES_2.1.6_TO_2.1.10.md`). Không dựa vào README cũ hoặc source code cũ.

Để phản ánh chính xác cấu trúc nghiệp vụ (Business Domain) của Favi theo tài liệu 2.1.x, chúng ta chia Domain Model thành các **Modules** tương ứng với module được định nghĩa trong tài liệu Use Case.

## 1. Các Module Chính (Domain Contexts)

Dựa trên header `**Module**` trong các file markdown:

1.  **Authentication & Profiles** (từ `UC_2.1.1`, `UC_2.1.2`, `UC_2.1.6`, `UC_2.1.9`)
    -   Quản lý `Profile`, quan hệ `Follow`, và chặn/report `UserModeration`.
    -   Đây là lõi của User Graph.

2.  **Content Management** (từ `UC_2.1.3`, `UC_2.1.5`)
    -   Quản lý nội dung bài viết (`Post`, `PostMedia`).
    -   Quản lý Bookmark/Danh sách (`Collection`, `CollectionItem`).
    -   Xử lý logic ẩn bài viết (`HiddenPost`).

3.  **Communication** (từ `UC_2.1.7`)
    -   Hệ thống Chat thời gian thực.
    -   Quan trọng: `Conversation` (Hội thoại) và pivot table `UserConversation` để quản lý người tham gia và trạng thái đọc (`LastReadMessageId`).

4.  **Communities** (từ `UC_2.1.12`)
    -   Quản lý nhóm (`Group`).
    -   Phân quyền thành viên (`GroupMember`) và duyệt yêu cầu vào nhóm (`GroupJoinRequest`).

5.  **Notifications** (từ `UC_2.1.8`)
    -   Hệ thống thông báo (`Notification`) tập trung.

6.  **Professional Tools** (từ `UC_2.1.11`)
    -   Mở rộng cho Creator (`ProfessionalProfile`).
    -   Số liệu (`Insight`) và Quảng cáo (`AdCampaign`).

7.  **Administration** (từ `UC_2.1.10`, `UC_2.1.13`)
    -   Quản lý báo cáo vi phạm (`Report`) đối với User hoặc Content.

## 2. Cách Vẽ và Cập Nhật

File PlantUML mẫu: `diagrams/favi_domain_model.puml`

### Quy tắc cập nhật:
1.  **Chỉ thêm Entity có trong Use Case 2.1.x**: Nếu Use Case không nhắc đến (ví dụ: các tính năng cũ đã bỏ), không đưa vào Domain Model.
2.  **Đặt đúng Package**: Luôn đặt Entity vào đúng `package "Module Name"` để dễ nhìn.
3.  **Quan hệ rõ ràng**:
    -   Dùng `Profile "1" -- "*" Post` (Association) cho quan hệ sở hữu/tác giả.
    -   Dùng `Post "1" *-- "*" PostMedia` (Composition) cho quan hệ cha-con chặt chẽ (xóa cha mất con).

### Ví dụ cú pháp:
```plantuml
package "Communities" {
    class Group {
        Name
        Privacy
    }
    class GroupMember {
        Role (Admin/Member)
    }
}
Group "1" -- "*" GroupMember
```

## 3. Kiểm tra tính đúng đắn
Khi sửa đổi, hãy mở file Markdown Use Case tương ứng để đối chiếu (ví dụ sửa phần Chat thì mở `UC_2.1.7_Chat.md`). Kiểm tra mục **Database Tables** ở đầu mỗi file Use Case để đảm bảo không bỏ sót bảng quan trọng nào.
