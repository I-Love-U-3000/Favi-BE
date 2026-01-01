# Phân tích chênh lệch hệ thống (Gap Analysis) - FINAL

Tài liệu này xác nhận trạng thái **Code thực tế** so với **Đặc tả Use Case (3.2.2.x)**. Đây là bản phân tích cuối cùng để định hướng hoàn thiện sản phẩm.

## I. ADMIN MODULE (UC-AD)

### 1. Kiểm duyệt & AI (UC-AD-01, 02)
| Tính năng | Trạng thái Code | Đánh giá | Yêu cầu hành động (Target State) |
| :--- | :--- | :--- | :--- |
| **Audit Log** | **Có (Cơ bản)**. | Đã có `AuditService` và gọi trong `AdminDeleteAsync`. Tuy nhiên chưa phủ hết các hành động (VD: Unban, Resolve Report). | - Gọi `AuditService.LogAsync` cho **mọi** hành động thay đổi dữ liệu của Admin.<br>- Chuẩn hóa format `Notes` và `Reason`. |
| **AI Auto Blur** | **Chưa có**. | Controller chỉ nhận file và lưu, không có bước check nội dung nhạy cảm. | - Tích hợp AI Service (AWS Rekognition/Azure Vision) để check ảnh khi Upload.<br>- Thêm cờ `IsSensitive` va `BlurUrl` vào bảng Post/Image. |

### 2. Xử lý báo cáo (UC-AD-03)
| Tính năng | Trạng thái Code | Đánh giá | Yêu cầu hành động (Target State) |
| :--- | :--- | :--- | :--- |
| **Transaction** | **Rời rạc**. | Các bước "Xóa bài" và "Đổi trạng thái Report" là 2 API riêng biệt. | - **Database Transaction**: Gộp 3 bước (Xóa bài + Đổi Status Report + Bắn Notify) vào 1 API duy nhất.<br>- Đảm bảo tính nguyên vẹn dữ liệu (ACID). |
| **Notify Reporter**| **Chưa có**. | Khi Admin xử lý xong, người báo cáo không nhận được thông báo gì. | - Thêm logic gửi Notification cho Reporter: "Cảm ơn bạn đã báo cáo, chúng tôi đã xử lý...". |

### 3. Giám sát hệ thống (UC-AD-05)
| Tính năng | Trạng thái Code | Đánh giá | Yêu cầu hành động (Target State) |
| :--- | :--- | :--- | :--- |
| **System Check** | **Sơ khai**. | Chỉ có `/health` trả về string/json đơn giản. | - Cài đặt `AspNetCore.HealthChecks.UI` dashboard.<br>- Config check sâu: DB connection, Redis status, Disk space. |
| **Logs API** | **Chưa có**. | Không có API để xem log lỗi hệ thống từ Admin Panel. | - Viết API `GET /api/admin/logs` truy xuất bảng SystemLogs. |

---

## II. USER MODULE (UC-US)

### 1. Tương tác & Riêng tư (UC-US-05)
| Tính năng | Trạng thái Code | Đánh giá | Yêu cầu hành động (Target State) |
| :--- | :--- | :--- | :--- |
| **Block User** | **MISSING (Nghiêm trọng)**. | Không tìm thấy bảng `UserBlocks` hay Controller xử lý Block. | - Tạo bảng `UserBlock` (BlockerId, BlockedId).<br>- Thêm API Block/Unblock.<br>- **QUAN TRỌNG**: Update toàn bộ các query `GetFeed`, `Search`, `GetComments` để loại trừ nội dung từ user bị block. |

### 2. Bình luận (UC-US-04)
| Tính năng | Trạng thái Code | Đánh giá | Yêu cầu hành động (Target State) |
| :--- | :--- | :--- | :--- |
| **Reply Comment**| **Logic chưa tối ưu**. | API hỗ trợ `ParentId`, nhưng `GetByPost` trả về danh sách phẳng (Flat List) phân trang. | - **Vấn đề**: Nếu Comment cha ở trang 1, Comment con ở trang 2 -> Frontend rất khó render cây comment.<br>- **Giải pháp**: 1. Chỉ load Comment cha (Root) khi phân trang. 2. Có API riêng `GET /comments/{id}/replies` để load comment con. |

### 3. Tìm kiếm & Chat (UC-US-03, 08)
| Tính năng | Trạng thái Code | Đánh giá | Yêu cầu hành động (Target State) |
| :--- | :--- | :--- | :--- |
| **Semantic Search**| **Đã có**. | `SearchService` đã gọi Vector DB. | - Cần verify cấu hình Vector DB (Pinecone/Chroma) có hoạt động thực tế không. |
| **Chat** | **Đã có**. | `ChatService` và `ChatHub` (SignalR) đã implement đầy đủ DM/Group. | - (Đã hoàn thiện, chỉ cần test tích hợp). |

---

## III. TECHNICAL DEBT (NỢ KỸ THUẬT)

### 1. Xử lý lỗi (Error Handling)
- **Hiện tại**: Code thường catch exception và log, đôi khi nuốt lỗi hoặc trả về 500 chung chung.
- **Mục tiêu**: Áp dụng `Global Exception Handler` để trả về Error Code chuẩn (VD: `ERR_USER_BLOCKED`, `ERR_RESOURCE_NOT_FOUND`) giúp FE hiển thị thông báo localized.

### 2. Upload Media
- **Hiện tại**: Upload trực tiếp trong Controller/Service của bài viết.
- **Mục tiêu**: Tách ra `MediaService` riêng nếu muốn tái sử dụng cho Chat, Avatar, Cover photo, tránh lặp code.
