5. KẾT LUẬN VÀ HƯỚNG PHÁT TRIỂN

5.1. Kết luận

Trong đồ án “Xây dựng Mạng xã hội chia sẻ hình ảnh Favi”, nhóm đã xây dựng thành công một nền tảng mạng xã hội tập trung vào trải nghiệm hình ảnh, kết nối cộng đồng và đảm bảo môi trường tương tác an toàn. Hệ thống cung cấp đầy đủ các nghiệp vụ cốt lõi của một mạng xã hội hiện đại như: thiết lập hồ sơ cá nhân, đăng tải và chia sẻ nội dung đa phương tiện, tương tác thời gian thực (Like, Comment, Chat), cùng các công cụ quản lý mạnh mẽ dành cho Quản trị viên.

Điểm nhấn của đồ án là việc tích hợp thành công các công nghệ AI vào quy trình xử lý nội dung: tự động phát hiện và làm mờ nội dung nhạy cảm (AI Blur) và tìm kiếm thông minh theo ngữ nghĩa (Vector Search), giúp nâng cao trải nghiệm và sự an toàn cho người dùng.

Kết quả kiểm thử cho thấy hệ thống hoạt động ổn định, đáp ứng được các yêu cầu phi chức năng về hiệu năng và bảo mật, giao diện thân thiện, dễ sử dụng, sẵn sàng cho việc triển khai và mở rộng quy mô.

5.2. Ưu điểm của đồ án

*   **Bao phủ nghiệp vụ mạng xã hội toàn diện:** Từ xác thực, quản lý Profile, đến các nghiệp vụ phức tạp về Feed, Tương tác và Chat Realtime.
*   **Kiến trúc hệ thống hiện đại & An toàn:** Sử dụng Microservices (cho module AI), Backend .NET Core hiệu năng cao, Frontend Next.js tối ưu SEO. Cơ chế bảo mật vững chắc với JWT và phân quyền RBAC.
*   **Tích hợp AI thực tiễn:** Giải quyết bài toán kiểm duyệt nội dung tự động bằng AI Vision và nâng cao khả năng tìm kiếm nội dung bằng Vector Database (Qdrant), vượt trội so với tìm kiếm từ khóa thông thường.
*   **Trải nghiệm người dùng (UX/UI) tốt:** Giao diện được thiết kế theo phong cách hiện đại (Minimalism), hỗ trợ Dark Mode và Responsive trên đa thiết bị.
*   **Cơ sở dữ liệu tối ưu:** Thiết kế CSDL quan hệ chặt chẽ (PostgreSQL) kết hợp với Vector DB, đảm bảo tính toàn vẹn dữ liệu và tốc độ truy vấn cao.

5.3. Hạn chế của đồ án

*   **Tính năng AI chưa đủ độ sâu:** Mô hình nhận diện nội dung nhạy cảm mới chỉ dừng lại ở việc phân loại ảnh và caption, chưa xử lý tốt các trường hợp video hoặc text phức tạp. Hệ thống gợi ý (Recommendation) chưa thực sự cá nhân hóa sâu theo hành vi người dùng.
*   **Quy mô dữ liệu và kiểm thử tải:** Hệ thống chưa được kiểm thử với lượng dữ liệu người dùng lớn (Big Data) và lượng truy cập đồng thời cao (High Concurrency) để đánh giá giới hạn chịu tải thực tế.
*   **Thiếu các tính năng video ngắn:** Chưa hỗ trợ định dạng video ngắn (Shorts/Reels) - xu hướng chủ đạo của mạng xã hội hiện nay.
*   **Trải nghiệm Realtime chưa tối đa:** Chưa tích hợp Video Call/Voice Call trong tính năng Chat.

5.4. Hướng phát triển

*   **Xây dựng hệ thống gợi ý tin (Newsfeed Recommendation Engine):** Ứng dụng Hybrid Filtering (kết hợp Collaborative Filtering và Content-based) để cá nhân hóa dòng tin, đề xuất nội dung phù hợp nhất với sở thích người dùng, giúp tăng thời gian on-site.
*   **Phát triển tính năng Video ngắn (Favi Reels):** Hỗ trợ đăng tải, chỉnh sửa và lướt xem video ngắn dọc màn hình, cạnh tranh trực tiếp với các nền tảng xu hướng hiện nay.
*   **Nâng cấp AI Moderator đa phương thức:** Mở rộng khả năng kiểm duyệt sang video (phân tích frame) và văn bản (NLP để phát hiện hate speech/spam), xây dựng môi trường mạng xã hội sạch tự động.
*   **Mở rộng nền tảng Mobile App:** Phát triển ứng dụng Native (React Native/Flutter) để tối ưu hóa trải nghiệm trên thiết bị di động, hỗ trợ thông báo đẩy (Push Notification) và truy cập offline.
*   **Tích hợp tính năng kiếm tiền (Monetization):** Cho phép người sáng tạo nội dung nhận Donate, hoặc triển khai hệ thống quảng cáo native ads để tạo nguồn thu cho nền tảng.
*   **Nâng cao tính năng Chat:** Bổ sung tính năng gọi thoại (Voice Call) và gọi video (Video Call) sử dụng WebRTC để hoàn thiện trải nghiệm kết nối.
