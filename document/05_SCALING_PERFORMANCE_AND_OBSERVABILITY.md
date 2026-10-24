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

## 3. Tối Ưu Truy Vấn & Xử Lý Dữ Liệu PostgreSQL (Query & Processing Optimization)

Để phục vụ hàng triệu tương tác của mạng xã hội mà không làm nghẽn I/O cơ sở dữ liệu, Favi-BE áp dụng các nguyên tắc tối ưu tầng dữ liệu sau:

### 3.1 Chiến Lược Đánh Chỉ Mục (PostgreSQL Indexing Strategy)
1. **Chỉ mục kết hợp (Composite Indexes)**:
   - Được thiết lập trên các bảng có lưu lượng truy vấn đọc cao nhất theo cặp điều kiện lọc và sắp xếp:
     - Bảng `Posts`: Index `("UserId", "CreatedAt" DESC)` tối ưu cho truy vấn xem timeline/trang cá nhân của một người dùng.
     - Bảng `Comments`: Index `("PostId", "CreatedAt" ASC)` và `("ParentCommentId")` giúp dựng cây bình luận nhanh chóng mà không cần quét toàn bộ bảng.
     - Bảng `Follows`: Unique composite index `("FollowerId", "FolloweeId")` vừa bảo đảm tính duy nhất vừa tăng tốc kiểm tra trạng thái quan hệ follow giữa hai người dùng.
2. **Chỉ mục Khóa Ngoại (Foreign Key Indexes)**:
   - PostgreSQL không tự động tạo index trên các cột Foreign Key. Tất cả khóa ngoại liên kết (`AuthorId`, `PostId`, `ConversationId`, `CollectionId`) đều được gắn B-Tree index để tránh Full Table Scan khi thực hiện phép JOIN hoặc khi xử lý cascade delete.
3. **Chỉ mục một phần (Partial Indexes)**:
   - Giảm dung lượng index và tăng tốc độ ghi bằng cách chỉ đánh chỉ mục trên các bản ghi có ý nghĩa nghiệp vụ tức thời:
     - Bảng `Stories`: `CREATE INDEX idx_stories_active ON "Stories" ("CreatedAt") WHERE "ExpiresAt" > NOW();`
     - Bảng hỗ trợ xóa mềm (Soft Delete): `CREATE INDEX ... WHERE "IsDeleted" = false;`
4. **Chỉ mục Chuyên dụng (GIN / Full-Text / Vector Offloading)**:
   - Đối với tìm kiếm chuỗi ký tự gần đúng (fuzzy search username/tags), sử dụng `pg_trgm` GIN index.
   - Đối với tìm kiếm ngữ nghĩa ảnh và bài viết (vector embeddings), tách biệt hoàn toàn sang dịch vụ `vector-index-api` (FastAPI) để tránh tạo áp lực tính toán khoảng cách cosine lên PostgreSQL chính.

### 3.2 Phân Trang Hiệu Năng Cao: Keyset / Cursor-based Pagination
- **Vấn đề của Offset/Limit (`Skip().Take()`)**: Khi người dùng lướt feed sâu (ví dụ trang 1,000, tương đương `OFFSET 20000 LIMIT 20`), database vẫn phải quét qua 20,000 bản ghi trong B-Tree trước khi trả về 20 bản ghi cuối (độ phức tạp $O(N)$ tăng dần theo độ sâu trang).
- **Giải pháp Keyset Pagination (Cursor)**:
  - Tất cả API bảng tin (`/api/posts/feed`, `/api/posts/user/{id}`, `/api/comments`) sử dụng con trỏ thời gian kết hợp khóa chính:
    ```sql
    SELECT * FROM "Posts"
    WHERE ("CreatedAt", "Id") < (@cursorCreatedAt, @cursorId)
    ORDER BY "CreatedAt" DESC, "Id" DESC
    LIMIT 20;
    ```
  - Cơ chế này cho phép database nhảy thẳng tới node lá của B-Tree index với độ phức tạp $O(1)$, giữ độ trễ truy vấn ổn định dù feed có hàng triệu bài viết.

### 3.3 Phòng Chống N+1 Query & Hiện Tượng Nhân Bản Dữ Liệu (Cartesian Explosion)
1. **Explicit Projections**:
   - Query Handlers luôn dùng `.Select(x => new PostDto { ... })` để chỉ định danh sách trường cần lấy. Không sử dụng `Include()` toàn bộ entity nếu chỉ cần hiển thị vài trường thông tin.
2. **Split Queries khi tải quan hệ đa cấp**:
   - Khi bắt buộc phải tải danh sách quan hệ 1-N (ví dụ: Post cùng các Media và Reactions), sử dụng `.AsSplitQuery()`:
     ```csharp
     var post = await _context.Posts
         .AsNoTracking()
         .AsSplitQuery()
         .Include(p => p.Media)
         .Include(p => p.Tags)
         .FirstOrDefaultAsync(p => p.Id == postId, cancellationToken);
     ```
   - EF Core sẽ tách thành nhiều câu truy vấn SQL độc lập gửi theo lô thay vì một lệnh `JOIN` khổng lồ sinh ra Cartesian product (nhân chéo bản ghi gây phình to RAM và network bandwidth).

### 3.4 Xử Lý Khối & Truy Vấn Hàng Loạt (Batching & Bulk Operations)
1. **Batch Chunking trong Seed & Sync**:
   - Khi chèn hoặc cập nhật hàng nghìn bản ghi (như trong Deterministic Seed Pipeline), hệ thống chia dữ liệu thành các khối (chunks) từ **500 - 1000 bản ghi** và gọi `AddRangeAsync()` kèm một lệnh `SaveChangesAsync()` duy nhất cho mỗi khối thay vì gọi trong từng vòng lặp.
2. **Bulk Update / Delete trực tiếp**:
   - Với các tác vụ quản trị hoặc cập nhật hàng loạt trạng thái (ví dụ: đánh dấu thông báo đã đọc, xóa tin hết hạn), tận dụng `ExecuteUpdateAsync()` và `ExecuteDeleteAsync()` (EF Core 7+) để sinh câu lệnh SQL UPDATE/DELETE trực tiếp trên DB mà không cần load thực thể vào bộ nhớ.

### 3.5 Giám Sát Truy Vấn Chậm (Slow Query Profiling & EXPLAIN ANALYZE)
1. **Quy chuẩn kiểm tra câu truy vấn**:
   - Khi thêm mới hoặc tối ưu truy vấn phức tạp, nhà phát triển bắt buộc chạy `EXPLAIN (ANALYZE, BUFFERS)` trong môi trường staging:
     - Mục tiêu: Triệt tiêu hoàn toàn `Seq Scan` (quét toàn bảng) trên các bảng lớn hơn 10,000 dòng.
     - Đảm bảo các phép lọc điều kiện sử dụng `Index Scan` hoặc `Bitmap Index Scan`.
2. **Cấu hình phát hiện Slow Query trên Production**:
   - PostgreSQL được cấu hình tự động ghi log cảnh báo các câu lệnh có thời gian thực thi vượt quá ngưỡng:
     ```
     log_min_duration_statement = 200 # Log mọi query chạy lâu hơn 200ms
     ```
   - Log này được chuỗi giám sát thu thập và kích hoạt cảnh báo tới đội ngũ kỹ thuật.

---

## 4. Cấu Hình Cơ Sở Dữ Liệu PostgreSQL Chịu Tải

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

## 5. Hệ Thống Giám Sát & Đo Lường (Observability Stack)

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

### 5.1 Thu Thập Metrics với Prometheus
- **Endpoint NGINX Status**: `/nginx_status` nội bộ được đọc bởi `favi-nginx-exporter`.
- **Chỉ số đo lường chính**:
  - `nginx_connections_active`: Số lượng kết nối đang mở thời gian thực.
  - `nginx_http_requests_total`: Tổng số request theo từng mã phản hồi (2xx, 4xx, 5xx).
  - Tần suất các request bị rate limit chặn (HTTP 429).
  - Trạng thái phản hồi của từng upstream replica (`up_status`, `up_time`).

### 5.2 Giao Diện Dashboard Grafana
- Truy cập tại: `http://localhost:3050` (Tài khoản mặc định: `admin` / `admin`).
- Các biểu đồ được cấu hình sẵn:
  - Throughput (RPS - Requests Per Second).
  - Tỷ lệ lỗi 5xx so với 2xx (Error Budget).
  - Độ trễ phản hồi (Response Latency p50, p95, p99).
  - Tình trạng bộ nhớ RAM và CPU của các container.

### 5.3 Endpoint Kiểm Tra Sức Khỏe (Health Checks)
- URL: `GET /health` (bỏ qua xác thực và rate limit).
- Kiểm tra sức khỏe của: Kết nối PostgreSQL, kết nối Redis, và trạng thái Vector API.
- Docker compose sử dụng health check này để xác định replica đã sẵn sàng nhận traffic hay chưa.
