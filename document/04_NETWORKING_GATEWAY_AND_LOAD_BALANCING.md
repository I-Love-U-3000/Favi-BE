# 04 — NGINX API Gateway, Load Balancing & Mạng Lưới (Favi-BE)

## 1. Vai Trò Của NGINX Gateway (L7 Reverse Proxy & Load Balancer)

Trong kiến trúc của Favi, NGINX đóng vai trò là **cửa ngõ duy nhất (Single Point of Ingress)** lắng nghe tại cổng `5000` (mapped từ port `80` trong container). 
Tất cả client từ bên ngoài (Web Next.js, Mobile App, Postman) đều gọi qua Gateway này.

Cấu hình được chia thành 2 file:
- `gateway/nginx.conf`: Thiết lập worker, buffer, gzip, các vùng rate limiting zones và connection upgrade cho WebSockets.
- `gateway/conf.d/gateway.conf`: Định nghĩa các upstream clusters, định tuyến (routing), bảo mật headers, và passive failover.

---

## 2. Các Cụm Upstream & Thuật Toán Phân Tải (Load Balancing)

```
                              [ NGINX Gateway ]
                                      │
            ┌─────────────────────────┼─────────────────────────┐
            │ (least_conn)            │ (ip_hash)               │ (least_conn)
            ▼                         ▼                         ▼
┌─────────────────────────┐ ┌─────────────────────────┐ ┌─────────────────────────┐
│    favi_api_cluster     │ │  favi_signalr_cluster   │ │   vector_api_cluster    │
│  (REST API Endpoints)   │ │  (WebSockets / SignalR) │ │ (FastAPI + AI Embeddings)│
│  - favi-api-1:8080      │ │  - favi-api-1:8080      │ │  - vector-index-api:8080 │
│  - favi-api-2:8080      │ │  - favi-api-2:8080      │ │                         │
│  Keepalive: 64 conn     │ │  Keepalive: 64 conn     │ │  Keepalive: 32 conn     │
└─────────────────────────┘ └─────────────────────────┘ └─────────────────────────┘
```

### 2.1 Cụm REST API (`favi_api_cluster`)
- **Thuật toán**: `least_conn` (ưu tiên chuyển request tới replica có ít active connection nhất).
- **Thành viên**: `favi-api-1:8080` và `favi-api-2:8080`.
- **Cơ chế chịu lỗi (Passive Failover)**:
  ```nginx
  proxy_next_upstream error timeout invalid_header http_502 http_503 http_504;
  proxy_next_upstream_tries 2;
  proxy_next_upstream_timeout 10s;
  ```
  Nếu một replica bị sập, khởi động lại, hoặc trả về mã lỗi 502/503/504, NGINX sẽ tự động thử lại trên replica còn lại trong vòng 10 giây mà không làm gián đoạn người dùng.
- **Keepalive connections**: Giữ 64 kết nối mở sẵn để tránh overhead bắt tay TCP liên tục.

### 2.2 Cụm SignalR Realtime (`favi_signalr_cluster`)
- **Vấn đề**: Giao thức SignalR thực hiện một bước đàm phán HTTP Negotiate (`/negotiate`) trước khi nâng cấp thành kết nối WebSocket kéo dài. Nếu bước negotiate rơi vào node 1 nhưng WebSocket lại kết nối vào node 2, kết nối sẽ bị từ chối (`404 Connection ID not found`).
- **Giải pháp**: Sử dụng thuật toán **`ip_hash` (Sticky Sessions)**. Thuật toán này băm địa chỉ IP của Client để đảm bảo mọi request từ cùng một Client luôn đến đúng 1 replica duy nhất.
- **Cấu hình WebSocket**:
  - `proxy_http_version 1.1;`
  - `proxy_set_header Upgrade $http_upgrade;`
  - `proxy_set_header Connection $connection_upgrade;`
  - `proxy_read_timeout 3600s;` (giữ kết nối sống 1 tiếng)
  - `proxy_buffering off;` (tắt buffer để truyền tải tin nhắn tức thì)

### 2.3 Cụm Vector & AI API (`vector_api_cluster`)
- Chuyển hướng các request tìm kiếm vector và kiểm duyệt nội dung phản cảm:
  `^/api/v1/(search/vector|nsfw)` sang container FastAPI `vector-index-api:8080`.

---

## 3. Hệ Thống Giới Hạn Tần Suất (Rate Limiting Tiers)

NGINX sử dụng bộ nhớ dùng chung (10MB cho mỗi zone, lưu trữ được ~160.000 IP riêng biệt) để ngăn chặn tấn công DoS, Brute-force và lạm dụng API:

| Zone Name | Rate Cho Phép | Burst (Hàng Đợi) | Endpoints Áp Dụng | Mục Đích |
|---|---|---|---|---|
| `auth_limit` | **10 requests / phút** | `burst=5 nodelay` | `/api/auth/(login\|register\|forgot-password\|reset-password)` | Chống Brute-force mật khẩu & spam tạo tài khoản rác. |
| `search_limit` | **30 requests / phút** | `burst=10 nodelay` | `/api/v1/(search/vector\|nsfw)` | Bảo vệ tài nguyên tính toán nặng của model AI CLIP/Vector. |
| `api_limit` | **150 requests / phút** | `burst=30 nodelay` | Toàn bộ các API nghiệp vụ khác (`/api/*`) | Chống cào dữ liệu (scraping) và flooding request. |

- Khi vượt quá ngưỡng cho phép, NGINX trả về ngay lập tức HTTP **`429 Too Many Requests`**.

---

## 4. Offloading Tài Nguyên Tĩnh & File Uploads

### 4.1 Offload Tài Nguyên Tĩnh Hạt Giống (`/seed-assets/`)
- Thay vì để .NET runtime đọc và stream file ảnh từ ổ cứng làm tốn luồng worker của ASP.NET Core, NGINX đọc trực tiếp từ thư mục mount `/var/www/seed-assets/`.
- Cấu hình cache trình duyệt vĩnh viễn:
  ```nginx
  location /seed-assets/ {
      alias /var/www/seed-assets/;
      expires 7d;
      add_header Cache-Control "public, max-age=604800, immutable";
      access_log off;
  }
  ```

### 4.2 Tải Lên Đa Phương Tiện (File Upload Limit)
- Mặc định toàn hệ thống giới hạn body size là `client_max_body_size 2M;`.
- Riêng các endpoint upload bài viết, avatar, media:
  `location ~* ^/api/(posts|profile/avatar|media|upload)` được mở rộng lên **`25M`** (`client_max_body_size 25M;`).

---

## 5. Truy Vết Phân Tán (Distributed Tracing) & Bảo Mật

- **Header `X-Request-ID`**: NGINX tự động sinh một mã UUID ngẫu nhiên `$request_id` cho mỗi request (hoặc nhận từ client) và chuyển tiếp tới backend qua header `X-Request-ID`. Backend Serilog log mã này vào mọi trace, và Gateway cũng trả lại header này trong response cho Client. Khi có lỗi, chỉ cần tìm theo Request ID là truy ra toàn bộ log của tất cả các tầng.
- **Bảo mật Header**:
  - `X-Content-Type-Options: nosniff` (chống MIME-type sniffing).
  - `X-Frame-Options: DENY` (chống Clickjacking).
- **Gzip Compression**: Nén tự động các payload text, JSON, javascript, XML khi dung lượng lớn hơn 1024 bytes (mức nén level 6).
