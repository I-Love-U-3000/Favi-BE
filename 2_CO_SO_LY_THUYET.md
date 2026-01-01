# 2. CƠ SỞ LÝ THUYẾT

## 2.1. Ngôn ngữ lập trình sử dụng

*   **C# (C Sharp):** Ngôn ngữ lập trình hướng đối tượng hiện đại, mạnh mẽ, được Microsoft phát triển. Trong đồ án này, C# được sử dụng để xây dựng toàn bộ Back-end API trên nền tảng .NET 8, tận dụng các tính năng mới nhất về hiệu năng và cú pháp (Record types, Pattern matching).
*   **TypeScript:** Là siêu tập hợp (superset) của JavaScript, bổ sung tính năng định kiểu tĩnh (static typing). TypeScript được sử dụng cho toàn bộ Front-end (Next.js) giúp phát hiện lỗi sớm trong quá trình phát triển và giúp mã nguồn dễ bảo trì hơn.
*   **SQL (Structured Query Language):** Ngôn ngữ truy vấn dữ liệu có cấu trúc, được sử dụng để làm việc với hệ quản trị cơ sở dữ liệu PostgreSQL.

## 2.2. Công cụ và môi trường phát triển

*   **Visual Studio 2022:** IDE chuyên dụng để phát triển ứng dụng .NET, hỗ trợ debug, quản lý NuGet package và refactoring mã nguồn hiệu quả.
*   **Visual Studio Code:** Trình soạn thảo mã nguồn nhẹ, mạnh mẽ, được sử dụng chính để phát triển Front-end (Next.js) và chỉnh sửa các file cấu hình (Docker, JSON).
*   **Git & GitHub:** Hệ thống quản lý phiên bản phân tán (VCS) và nền tảng lưu trữ mã nguồn, hỗ trợ làm việc nhóm (Teamwork) và theo dõi lịch sử thay đổi (Commit history).
*   **Docker Desktop:** Công cụ quản lý container, giúp thiết lập môi trường phát triển đồng nhất (Database, Redis, Qdrant) mà không cần cài đặt trực tiếp lên máy cá nhân.
*   **Postman:** Công cụ hỗ trợ kiểm thử API (API Testing), giúp gửi các request HTTP (GET, POST, PUT, DELETE) để kiểm tra dữ liệu trả về từ Back-end.
*   **Qdrant Dashboard:** Công cụ giao diện web để quản lý và truy vấn dữ liệu vector trong Qdrant.

## 2.3. Công cụ giao diện và hỗ trợ người dùng

*   **Next.js 15 (React Framework):** Framework mạnh mẽ dựa trên thư viện React, hỗ trợ Server-Side Rendering (SSR) giúp tối ưu hóa SEO và tăng tốc độ tải trang ban đầu. Next.js cung cấp cơ chế Routing linh hoạt (App Router) và tích hợp tốt với TypeScript.
*   **Tailwind CSS:** Framework CSS ưu tiên tiện ích (Utility-first), cho phép xây dựng giao diện nhanh chóng bằng cách tổ hợp các class có sẵn ngay trong mã HTML/JSX mà không cần viết các file CSS rời rạc.
*   **Lucide React:** Bộ thư viện icon nhẹ, đẹp mắt và đồng bộ, được sử dụng để làm đẹp giao diện người dùng.

## 2.4. Framework / Nền tảng Back-end

*   **ASP.NET Core 8 Web API:** Nền tảng mã nguồn mở, đa nền tảng (Cross-platform) để xây dựng các dịch vụ RESTful API hiệu năng cao.
    *   **Entity Framework Core:** ORM (Object-Relational Mapper) giúp thao tác với cơ sở dữ liệu thông qua các đối tượng C# thay vì viết câu lệnh SQL thô.
    *   **Dependency Injection (DI):** Cơ chế tích hợp sẵn giúp quản lý sự phụ thuộc giữa các thành phần, giúp mã nguồn lỏng lẻo (loose coupling) và dễ kiểm thử.
    *   **JWT Authentication:** Middleware xác thực bảo mật dựa trên Token.

## 2.5. Cơ sở dữ liệu

*   **PostgreSQL:** Hệ quản trị cơ sở dữ liệu quan hệ (RDBMS) mã nguồn mở mạnh mẽ, dùng để lưu trữ dữ liệu chính của hệ thống như: Người dùng, Bài viết, Bình luận, Tin nhắn. PostgreSQL nổi tiếng về độ ổn định, tuân thủ chuẩn ACID và hỗ trợ các truy vấn phức tạp.
*   **Redis:** Hệ quản trị cơ sở dữ liệu in-memory (lưu trong RAM) tốc độ cực cao. Trong đồ án, Redis được dùng làm:
    *   **Cache:** Lưu tạm các dữ liệu hay truy xuất (như Newsfeed) để giảm tải cho Database chính.
    *   **Pub/Sub:** Hỗ trợ tính năng Chat thời gian thực.
*   **Qdrant:** Cơ sở dữ liệu Vector (Vector Database) chuyên dụng. Qdrant lưu trữ các vector đại diện (Embeddings) của hình ảnh và văn bản, phục vụ cho tính năng Tìm kiếm ngữ nghĩa (Semantic Search) và Gợi ý nội dung (Recommendation System).

## 2.6. Triển khai (Deployment)

*   **Docker & Docker Compose:** Đóng gói toàn bộ ứng dụng (Front-end, Back-end) và các dịch vụ phụ trợ (Postgres, Redis, Qdrant) thành các Container độc lập. Điều này đảm bảo ứng dụng "chạy đúng ở mọi nơi" (Build once, run anywhere), loại bỏ lỗi "works on my machine" và đơn giản hóa quá trình triển khai lên máy chủ.
