| STT | Tên bảng | Mô tả |
| :--- | :--- | :--- |
| 1 | `USERS` | Bảng quản lý thông tin đăng nhập và xác thực (Auth), được quản lý bởi Supabase Auth (hoặc Identity). |
| 2 | `PROFILES` | Hồ sơ người dùng chi tiết (Avatar, Bio, Social Links), liên kết 1-1 với `USERS`. |
| 3 | `POSTS` | Lưu trữ bài viết chính của người dùng (Caption, Timestamp). |
| 4 | `POST_MEDIAS` | Lưu trữ danh sách file đa phương tiện (Ảnh/Video) đính kèm bài viết. |
| 5 | `COMMENTS` | Lưu trữ bình luận của người dùng trên bài viết, hỗ trợ cấu trúc phân cấp (Reply). |
| 6 | `COLLECTIONS` | Danh mục/Album lưu trữ bài viết do người dùng tự tạo (Public/Private). |
| 7 | `POST_COLLECTIONS` | Bảng trung gian liên kết n-n giữa `POSTS` và `COLLECTIONS` (Một bài viết nằm trong nhiều bộ sưu tập). |
| 8 | `TAGS` | Lưu trữ các thẻ/chủ đề (Hashtag) để phân loại bài viết. |
| 9 | `POST_TAGS` | Bảng trung gian liên kết n-n giữa `POSTS` và `TAGS`. |
| 10 | `REACTIONS` | Lưu trữ hành động tương tác (Like/Love/Haha) của User lên Post, Comment hoặc Collection. |
| 11 | `SOCIAL_LINKS` | Lưu trữ các liên kết mạng xã hội của người dùng (FB, Ins, Web). |
| 12 | `FOLLOWS` | Bảng tự liên kết (Self-referencing) lưu mối quan hệ theo dõi giữa `Follower` và `Followee`. |
| 13 | `REPORTS` | Lưu trữ vé báo cáo vi phạm từ người dùng đối với nội dung hoặc người dùng khác. |
| 14 | `USER_MODERATIONS` | Lịch sử xử lý vi phạm của Admin đối với User (Ban/Unban, lý do). |
| 15 | `ADMIN_ACTIONS` | Nhật ký hành động của Admin (Audit Log) ghi lại mọi thao tác xóa/sửa dữ liệu hệ thống. |
| 16 | `NOTIFICATIONS` | Lưu trữ thông báo của hệ thống gửi đến người dùng (Like mới, Comment mới...). |
| 17 | `CONVERSATIONS` | Lưu trữ các cuộc hội thoại Chat (hội thoại 1-1 hoặc Nhóm). |
| 18 | `USER_CONVERSATIONS` | Bảng trung gian liên kết User tham gia vào Conversation (lưu thời điểm tham gia, tin nhắn đã đọc). |
| 19 | `MESSAGES` | Lưu trữ nội dung tin nhắn trong cuộc hội thoại (Text, Media). |
| 20 | `USER_BLOCKS` | (**Target State**) Lưu danh sách chặn người dùng (User A chặn User B). |
| 21 | `SYSTEM_LOGS` | (**Target State**) Lưu log lỗi hệ thống (Error Log) phục vụ debugging. |
