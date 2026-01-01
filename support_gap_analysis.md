# Phân tích chênh lệch hệ thống (Gap Analysis) - Admin Module

Tài liệu này ghi nhận các điểm khác biệt giữa **Đặc tả Use Case (3.2.2_admin.md)** và **Source Code hiện tại (BE)**. Các tính năng dưới đây cần được bổ sung hoặc nâng cấp để đạt tự động hóa và độ ổn định cao nhất (Target State).

## 1. UC-AD-01 & 02: Moderation Actions (Ban User & Delete Content)
| Tính năng | Trạng thái Code | Yêu cầu bổ sung |
| :--- | :--- | :--- |
| **Audit Log** | **Sơ khai**. Một số chỗ chưa ghi log đầy đủ hoặc ghi không chuẩn format. | - Đảm bảo mọi hành động (Ban, Unban, Delete Post) đều phải gọi `AuditService.LogAsync`.<br>- Log phải chứa đủ: `AdminId`, `ActionType`, `Reason`, `TargetId`. |
| **AI Auto Blur** | **Chưa có**. | - Tích hợp AI Service (AWS/Azure) để check ảnh khi Upload.<br>- Thêm cờ `IsSensitive` va `BlurUrl` vào bảng Post.<br>- Logic FE hiển thị lớp phủ mờ. |

## 2. UC-AD-03: Manage User Reports (Transaction)
| Tính năng | Trạng thái Code | Yêu cầu bổ sung |
| :--- | :--- | :--- |
| **Accept Report (Gộp)** | **Rời rạc**. Admin đang phải xóa bài thủ công rồi mới đổi status report. | - **Database Transaction**: Cần gói gọn 3 bước trong 1 Transaction:<br>  1. `PostService.Delete` (Soft delete)<br>  2. `ReportService.UpdateStatus` (Resolved)<br>  3. `NotificationService.Send` (Báo cho user)<br>- Nếu 1 bước lỗi -> Rollback toàn bộ. |
| **Thông báo (Notify)** | **Chưa có**. | - Tự động bắn thông báo Realtime/System Notification cho người báo cáo khi Admin xử lý xong. |

## 3. UC-AD-05: Monitor System Logs & Health
| Tính năng | Trạng thái Code | Yêu cầu bổ sung |
| :--- | :--- | :--- |
| **Health Check UI** | **Sơ khai** (`/health` JSON). | - Cài đặt `AspNetCore.HealthChecks.UI` để có giao diện trực quan.<br>- Config check sâu: DB Query Ping, Redis Ping, Disk Space. |
| **System Logs API** | **Chưa có**. | - Viết API `GET /api/admin/logs`: Trả về log lỗi từ DB (`SystemLogs`) để Admin debug ngay trên Dashboard. |

## 4. Xử lý ngoại lệ chuẩn (Standardized Error Handling)
- **Hiện tại**: Trả về `500 Internal Server Error` hoặc `400` tùy hứng.
- **Yêu cầu theo Spec**:
    - **Global Filter**: Bắt các lỗi cụ thể và trả về Mã lỗi chuẩn trong Body response:
        - `ERR_DB_TIMEOUT` (Khi DB connection fail)
        - `ERR_RECORD_NOT_FOUND` (Thay vì null)
        - `ERR_UNAUTHORIZED_ACTION`
    - Điều này giúp FE hiển thị thông báo lỗi chính xác (VD: "Mất kết nối CSDL" thay vì "Lỗi hệ thống").
