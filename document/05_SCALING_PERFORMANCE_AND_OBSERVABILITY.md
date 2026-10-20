# 05 — Mở Rộng Hệ Thống, Hiệu Năng & Giám Sát (Favi-BE)

## 1. Chiến Lược Mở Rộng Ngang (Horizontal Scaling)

Favi-BE được thiết kế theo nguyên tắc **Stateless Applications**:
- **Không lưu trạng thái trong bộ nhớ Process**: Phiên làm việc (Session), token xác thực (JWT), khóa Idempotency đều được lưu trữ phân tán trên Redis hoặc mã hóa trong token client.
- **Dễ dàng tăng số lượng Replicas**:
  Trong `docker-compose.yml`, hệ thống mặc định chạy 2 replicas (`favi-api-1` và `favi-api-2`). Khi cần tăng thêm node:
  1. Khởi tạo thêm container `favi-api-3:8080`, `favi-api-4:8080`.
  2. Khai báo thêm vào upstream `favi_api_cluster` và `favi_signalr_cluster` trong `gateway/conf.d/gateway.conf`.
  3. Reload NGINX không downtime (`nginx -s reload`).

---

## 2. Tối Ưu Hiệu Năng Đọc/Ghi (Read/Write Segregation)

Để đạt thông lượng cao trên các tác vụ đọc (chiếm 90% lưu lượng của mạng xã hội như xem feed, xem profile), hệ thống tách bạch 2 luồng:

```
[ Command (Ghi) ] ──▶ EF Core ChangeTracker ──▶ Master PostgreSQL (Write)
                                                     │
[ Query (Đọc) ]   ──▶ EF Core .AsNoTracking() ───────┘ (Hiện tại)
                      │
                      ▼ (Lộ trình tối ưu hot paths)
                      Dapper Projections / Read Replicas (Future)
```

1. **Write Path**: EF Core tracking context với đầy đủ quan hệ domain, đảm bảo tính toàn vẹn và thực thi validation rules.
2. **Read Path**: Bắt buộc sử dụng `.AsNoTracking()` trong tất cả Query Handlers để:
   - Bỏ qua chi phí ChangeTracker overhead của EF Core.
   - Giảm đáng kể dung lượng RAM cấp phát cho Garbage Collection (GC).
   - Truy vấn chỉ `Select(x => new Dto { ... })` để chỉ tải các cột cần thiết từ DB.
3. **Caching Phân Tầng với Redis**:
   - Cache thông tin profile người dùng (`favi:profile:{id}`).
   - Cache các bài viết xu hướng (Trending Tags & Collections).
   - Vô hiệu hóa cache (Cache Invalidation) khi nhận được Event cập nhật tương ứng.

---

## 3. Cấu Hình Cơ Sở Dữ Liệu PostgreSQL Chịu Tải

Trong `docker-compose.yml`, dịch vụ `favi-postgres` được tinh chỉnh tối ưu hóa cho môi trường mạng xã hội nhiều kết nối:

```yaml
favi-postgres:
  image: postgres:15
  command: >
    postgres
    -c max_connections=250
    -c shared_buffers=512MB
    -c work_mem=16MB
    -c maintenance_work_mem=64MB
```

- **`max_connections=250`**: Đảm bảo đủ connection cho nhiều replica API cùng kết nối trong connection pool (`Maximum Pool Size=100;`).
- **`shared_buffers=512MB`**: Tăng kích thước bộ nhớ đệm trang dữ liệu của Postgres trong RAM.
- **`work_mem=16MB`**: Tăng tốc độ sắp xếp (ORDER BY), gom nhóm (GROUP BY) và băm (HASH JOIN) của các câu lệnh truy vấn bảng tin phức tạp.

---

## 4. Hệ Thống Giám Sát & Đo Lường (Observability Stack)

Hệ thống tích hợp sẵn chuỗi công cụ giám sát theo dõi hiệu năng theo thời gian thực:

```
[ NGINX Gateway ] ──▶ /nginx_status ──▶ [ favi-nginx-exporter ]
                                                    │ (Scrape)
                                                    ▼
[ .NET API /health ] ─────────────────────▶ [ favi-prometheus ] (Port 9090)
                                                    │
                                                    ▼
                                            [ favi-grafana ] (Port 3050)
                                            Dashboard: Request Rates,
                                            Latencies, Active Conns
```

### 4.1 Thu Thập Metrics với Prometheus
- **Endpoint NGINX Status**: `/nginx_status` nội bộ được đọc bởi `favi-nginx-exporter`.
- **Chỉ số đo lường chính**:
  - `nginx_connections_active`: Số lượng kết nối đang mở thời gian thực.
  - `nginx_http_requests_total`: Tổng số request theo từng mã phản hồi (2xx, 4xx, 5xx).
  - Tần suất các request bị rate limit chặn (HTTP 429).
  - Trạng thái phản hồi của từng upstream replica (`up_status`, `up_time`).

### 4.2 Giao Diện Dashboard Grafana
- Truy cập tại: `http://localhost:3050` (Tài khoản mặc định: `admin` / `admin`).
- Các biểu đồ được cấu hình sẵn:
  - Throughput (RPS - Requests Per Second).
  - Tỷ lệ lỗi 5xx so với 2xx (Error Budget).
  - Độ trễ phản hồi (Response Latency p50, p95, p99).
  - Tình trạng bộ nhớ RAM và CPU của các container.

### 4.3 Endpoint Kiểm Tra Sức Khỏe (Health Checks)
- URL: `GET /health` (bỏ qua xác thực và rate limit).
- Kiểm tra sức khỏe của: Kết nối PostgreSQL, kết nối Redis, và trạng thái Vector API.
- Docker compose sử dụng health check này để xác định replica đã sẵn sàng nhận traffic hay chưa.
