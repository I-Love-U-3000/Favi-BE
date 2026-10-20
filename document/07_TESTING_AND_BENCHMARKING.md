# 07 — Chiến Lược Kiểm Thử, Hiệu Năng & Đo Lường Tải (Testing & Benchmarking)

Tài liệu này quy định toàn diện chiến lược kiểm thử đa tầng (Multi-tier Testing Strategy) của nền tảng **Favi Backend (Favi-BE)**: từ kiểm định cấu trúc kiến trúc (Architecture Tests), kiểm thử chức năng (Smoke, Functional, Sanity) cho đến kiểm thử chịu tải, đột biến và độ bền dài hạn (Load, Stress, Spike, Soak) bằng **k6**, kết hợp đối soát trực quan trên **Prometheus & Grafana**.

---

## 1. Kim Tự Tháp Kiểm Thử (Testing Pyramid & Quality Gates)

Hệ thống tuân thủ mô hình kiểm thử phân cấp nghiêm ngặt nhằm phát hiện lỗi ở tầng thấp nhất có thể trước khi triển khai lên môi trường chịu tải:

```
                      / \
                     /   \      k6 Stress, Spike, Soak Tests (Breakpoints, Memory Leaks)
                    /     \     ────────────────────────────────────────────────────────
                   /       \    k6 Load Tests (SLA/SLO Verification, p95/p99 Latency)
                  /         \   ────────────────────────────────────────────────────────
                 /           \  k6 E2E, Functional & Sanity Tests (Cross-module Workflows)
                /             \ ────────────────────────────────────────────────────────
               /               \k6 Smoke Tests (Liveness & Fast CI Gate)
              /                 \───────────────────────────────────────────────────────
             /                   \Favi-BE.ArchitectureTests (Clean Arch & Privacy Guard)
            /                     \─────────────────────────────────────────────────────
           /                       \Favi-BE.UnitTests (Domain Logic & Invariants)
          /─────────────────────────\
```

### Bộ Quy Tắc Quality Gates:
1. **Gate 1 (Build/Local)**: Toàn bộ Unit Tests và Architecture Tests phải pass 100%. Bất kỳ vi phạm nào về ranh giới module hoặc rò rỉ dữ liệu qua Privacy Guard sẽ làm gãy build ngay lập tức.
2. **Gate 2 (Smoke CI)**: Bộ Smoke k6 chạy nhanh dưới 2 phút với 1–2 VUs để xác nhận tất cả endpoints cốt lõi phản hồi 200/OK.
3. **Gate 3 (Load & Resilience)**: Chạy k6 Load / Gateway test qua NGINX reverse proxy (`http://localhost:5000`), xác minh phân tải cân bằng giữa `favi-api-1` và `favi-api-2` với p95 < 250ms và tỷ lệ lỗi < 0.1%.

---

## 2. Kiểm Thử Kiến Trúc & Ranh Giới Module (`Favi-BE.ArchitectureTests`)

Sử dụng thư viện `NetArchTest.Rules` và `xUnit` tại project [`Favi-BE.ArchitectureTests`](../Favi-BE/Favi-BE.ArchitectureTests) để tự động hóa việc thực thi các quy tắc thiết kế phần mềm:

### 2.1 Các Quy Tắc Kiến Trúc Bắt Buộc
- **Module Isolation**: Mã nguồn của một Bounded Context (ví dụ `ContentDiscovery`) tuyệt đối **không được reference** trực tiếp namespace nội bộ hoặc DB Context của module khác (ví dụ `Engagement` hay `SocialGraph`).
- **Clean Architecture Dependency Rule**: Tầng `Domain` không được phụ thuộc vào `Application`, `Infrastructure` hay các thư viện bên thứ ba (trừ `BuildingBlocks.Domain`).
- **Query Immutability**: Mọi Query Handler bắt buộc chỉ đọc dữ liệu (`.AsNoTracking()`), không được phép kích hoạt `SaveChanges()` hoặc thay đổi trạng thái entity.
- **Privacy Guard Enforcement ([`PrivacyGuardTests.cs`](../Favi-BE/Favi-BE.ArchitectureTests/PrivacyGuardTests.cs))**: Đảm bảo các thuộc tính riêng tư (email, hashed password, private post content) không bao giờ bị lộ ra các public-facing DTOs.

### 2.2 Lệnh Thực Thi
```powershell
dotnet test Favi-BE/Favi-BE.ArchitectureTests/Favi-BE.ArchitectureTests.csproj --logger "console;verbosity=normal"
```

---

## 3. Kiểm Thử Đơn Vị (`Favi-BE.UnitTests`)

Nằm tại [`Favi-BE.UnitTests`](../Favi-BE/Favi-BE.UnitTests), tập trung kiểm thử các logic nghiệp vụ nghiệp vụ thuần túy (pure domain logic) mà không cần phụ thuộc vào database hay network:
- Kiểm tra tính hợp lệ của Domain Entities và Value Objects.
- Kiểm tra Business Invariants (ví dụ: không thể react 2 lần cùng một loại biểu cảm nếu chưa hủy biểu cảm cũ).
- Kiểm tra tính toán phân trang và logic sinh domain events.

### Lệnh Thực Thi
```powershell
dotnet test Favi-BE/Favi-BE.UnitTests/Favi-BE.UnitTests.csproj
```

---

## 4. Bộ Kiểm Thử Tải & Hiệu Năng k6 (`Favi-BE.API/k6-tests/`)

Favi-BE tích hợp sẵn bộ kịch bản k6 phong phú tại [`Favi-BE/Favi-BE.API/k6-tests/`](../Favi-BE/Favi-BE.API/k6-tests), được chia thành các nhóm chuyên biệt:

### 4.1 Danh Mục Các Bộ Kịch Bản k6

| Phân Loại | Thư Mục | Số Lượng Kịch Bản | Mục Đích Chính |
| :--- | :--- | :---: | :--- |
| **Gateway & LB** | `k6-tests/gateway-loadbalancer-test.js` | 1 | Kiểm tra phân tải `least_conn`, Keep-Alive connection pool, và phòng vệ HTTP 429 Rate Limiter. |
| **Smoke** | `k6-tests/smoke/` | 13 | Quét nhanh 13 nhóm nghiệp vụ: Auth, Feed, Reaction, Comment, Post CRUD, Stories, Chat, Follow... |
| **Sanity** | `k6-tests/sanity/` | 8 | Kiểm thử tính toàn vẹn dữ liệu và các luồng side-effects sau khi chạy Seed Pipeline. |
| **Functional** | `k6-tests/functional/` | 7 | Kiểm tra hành vi nghiệp vụ phức tạp: bảo mật quyền riêng tư, luồng chat song phương, fanout thông báo. |
| **Load** | `k6-tests/load/` | 8 | Đo lường SLA/SLO (Throughput, Latency p95/p99) với tải người dùng tăng dần và ổn định. |
| **Stress, Spike & Soak** | `k6-tests/stress-spike-soak/` | 12 | Thử thách giới hạn hệ thống: Stress (tìm điểm gãy), Spike (bùng nổ tải tức thì), Soak (chạy liên tục 2h+ chống rò rỉ RAM). |

---

### 4.2 Chi Tiết Các Kịch Bản Load Testing (`k6-tests/load/`)

1. **`scenario-1-auth-login-bulk-users.js`**: Đăng nhập đồng thời hàng loạt tài khoản người dùng đã được seed sẵn.
2. **`scenario-2-feed-refresh-steady-users.js`**: Mô phỏng người dùng liên tục cuộn và làm mới bảng tin cá nhân (`/api/posts/feed-with-reposts`).
3. **`scenario-2-1-feed-fanout-worstcase-power-users.js`**: Thử tải bảng tin của các tài khoản "Power User" có danh sách following lên đến hàng ngàn người.
4. **`scenario-3-reaction-steady-mix.js`**: Bắn đồng thời các biểu cảm (Like, Love, Haha, Wow, Sad, Angry) vào các bài viết.
5. **`scenario-4-like-unlike-rotating-users.js`**: Thao tác thả tim và hủy tim xoay vòng liên tục để kiểm tra race conditions và tính đúng đắn của Redis cache counters.
6. **`scenario-5-trending-post-heavy-read.js`**: Lưu lượng đọc áp đảo tập trung vào các bài viết Top Trending.
7. **`scenario-6-profile-scroll-steady.js`**: Cuộn xem trang cá nhân, lưới bài viết và danh sách bạn bè.
8. **`scenario-7-search-related-moderate.js`**: Tìm kiếm từ khóa và gợi ý bài viết liên quan.

---

### 4.3 Chi Tiết Các Kịch Bản Stress, Spike & Soak (`k6-tests/stress-spike-soak/`)

#### A. Stress (Đẩy Tải Tìm Điểm Gãy - Breakpoint)
- `scenario-a1-stress-auth-login-mass-users.js`: Đẩy lượng request login vượt ngưỡng để kiểm tra NGINX `auth_limit` burst.
- `scenario-a2-stress-feed-refresh-continuous-mass.js`: Tăng VUs đều đặn cho đến khi latency vượt 1000ms.
- `scenario-a3-stress-reaction-hotspot-mass-users.js`: Dồn toàn bộ tải phản ứng vào duy nhất 1 bài viết nóng.
- `scenario-a4-stress-noti-storm-hot-entity.js`: Bão thông báo dồn dập vào một thực thể.
- `scenario-a5-stress-write-path-mass-create.js`: Thử tải ghi dữ liệu đồng thời qua Transactional Outbox.
- `scenario-a6-stress-media-pipeline-partial-failures.js`: Mô phỏng tải upload ảnh và xử lý lỗi gián đoạn.

#### B. Spike (Bùng Nổ Lưu Lượng Tức Thời)
- `scenario-b1-spike-feed-traffic-sudden-jump.js`: Đột ngột nhảy từ 5 VUs lên 150 VUs trong 5 giây.
- `scenario-b2-spike-login-after-event.js`: Đám đông người dùng đăng nhập đồng loạt sau một sự kiện phát sóng.
- `scenario-b3-spike-trending-post-viral.js`: Bài viết bất ngờ đạt viral thu hút lượng đọc khổng lồ trong thời gian cực ngắn.

#### C. Soak (Kiểm Tra Độ Bền Dài Hạn - Long Run)
- `scenario-c1-soak-feed-reaction-mix-long-run.js`: Chạy hỗn hợp đọc feed và thả reaction ổn định trong 1–2 giờ để phát hiện rò rỉ bộ nhớ (Memory Leak) trong .NET GC và Redis cache connection leaks.
- `scenario-c2-soak-refresh-pattern-long-run.js`: Vòng lặp refresh đều đặn kéo dài.
- `scenario-c3-soak-consistency-long-run.js`: Đo lường độ trễ đồng bộ nhất quán cuối cùng (Eventual Consistency) giữa Write DB và Read Models.

---

## 5. Hướng Dẫn Thực Thi Kiểm Thử Chi Tiết

### 5.1 Điều Kiện Tiên Quyết
1. **Cluster Docker đang hoạt động**:
   ```powershell
   docker-compose up -d
   ```
2. **K6 CLI đã cài đặt**:
   ```powershell
   winget install GrafanaLabs.k6
   # Kiểm tra phiên bản:
   k6 version
   ```

---

### 5.2 Chạy Kịch Bản Gateway & Load Balancing
Kịch bản kiểm tra khả năng phân tải L7 qua Gateway (`http://localhost:5000`), giữ kết nối Keep-Alive và kích hoạt Rate Limit:

```powershell
cd Favi-BE/Favi-BE.API
k6 run k6-tests/gateway-loadbalancer-test.js
```

---

### 5.3 Chạy Từng Nhóm Kịch Bản Qua PowerShell Runner

Toàn bộ các thư mục test đều có sẵn script PowerShell tự động hóa (`run-all.ps1`):

#### 1. Chạy Smoke Tests (Kiểm tra nhanh toàn bộ chức năng):
```powershell
cd Favi-BE/Favi-BE.API
pwsh ./k6-tests/smoke/run-all.ps1 -BaseUrl http://localhost:5000 -ShowSummary
```

#### 2. Chạy Load Tests (Kiểm tra tải định lượng):
```powershell
cd Favi-BE/Favi-BE.API
pwsh ./k6-tests/load/run-all.ps1 -BaseUrl http://localhost:5000 -ShowSummary
```

#### 3. Chạy Stress & Spike Tests:
```powershell
cd Favi-BE/Favi-BE.API
pwsh ./k6-tests/stress-spike-soak/run-all.ps1 -BaseUrl http://localhost:5000 -ShowSummary
```

#### 4. Chạy Riêng Soak Test Kéo Dài 2 Giờ:
```powershell
cd Favi-BE/Favi-BE.API
pwsh ./k6-tests/stress-spike-soak/run-all.ps1 -BaseUrl http://localhost:5000 -SoakDuration 2h
```

---

### 5.4 Chạy Kịch Bản Độc Lập Nhanh (Standalone Scripts)

Nếu muốn chạy nhanh một kịch bản đơn lẻ đặt tại thư mục gốc backend:

```powershell
cd Favi-BE
# Test feed đọc cá nhân có reposts (yêu cầu login token):
k6 run load_test_feed.js

# Test breakpoint tải feed cho khách (guest):
k6 run load_test_feed_guest_breakpoint.js

# Test breakpoint tải feed cho user có token:
k6 run load_test_feed_auth_breakpoint.js

# Test hiệu năng đăng nhập auth:
k6 run load_test_login.js
```

---

## 6. Đối Soát Trực Quan Với Prometheus & Grafana

Trong suốt quá trình chạy k6, kỹ sư mở đồng thời giao diện giám sát để theo dõi các phản ứng nội tại của hệ thống:

```
[ k6 Load Generator ] ──(HTTP Load)──▶ [ NGINX Gateway :5000 ]
                                              │
                         ┌────────────────────┴────────────────────┐
                         ▼                                         ▼
                 [ favi-api-1 :8080 ]                      [ favi-api-2 :8080 ]
                         │                                         │
                         └────────────────────┬────────────────────┘
                                              ▼
                                     [ NGINX Exporter ]
                                              │ (Scrape /nginx_status)
                                              ▼
                                    [ Prometheus :9090 ]
                                              │
                                              ▼
                                     [ Grafana :3050 ]
                                 (Dashboard: favi-overview)
```

### 6.1 Bảng Điều Khiển Grafana
- **Địa chỉ truy cập**: `http://localhost:3050` (Tài khoản: `admin` / `admin`).
- **Dashboard**: **Favi Cluster Performance & Gateway Overview** (`/d/favi-overview`).
- **Cấu hình theo dõi**: Đặt khoảng thời gian **Last 5 minutes**, tần suất tự động cập nhật **5s**.

### 6.2 Các Chỉ Số Trọng Yếu Cần Giám Sát

| Chỉ Số Trong Grafana | Metric Prometheus | Ý Nghĩa Kỹ Thuật |
| :--- | :--- | :--- |
| **Request Throughput (RPS)** | `rate(nginx_http_requests_total[10s])` | Tốc độ xử lý request/giây thực tế của Gateway. |
| **Active Client Connections** | `nginx_connections_active` | Số lượng socket TCP client đang mở tới hệ thống. |
| **Waiting (Keep-Alive)** | `nginx_connections_waiting` | Số kết nối đang ở trạng thái nhàn rỗi (idle Keep-Alive), chứng minh cơ chế connection pooling của NGINX hoạt động tối ưu. |
| **Rate Limit 429 Hits** | Tăng trưởng mã trạng thái HTTP 429 | Xác nhận các đợt tấn công brute-force auth bị chặn đứng ở tầng Gateway mà không chạm tới .NET backend. |

---

## 7. Thử Nghiệm Độ Bền Failover Không Downtime (Chaos & High Availability)

Nhằm kiểm chứng chỉ thị `proxy_next_upstream` trong `gateway/conf.d/gateway.conf`:

```nginx
proxy_next_upstream error timeout invalid_header http_502 http_503 http_504;
proxy_next_upstream_tries 2;
```

### Quy Trình Thực Hiện Thử Nghiệm:
1. Bật k6 chạy tải liên tục trong 60 giây:
   ```powershell
   cd Favi-BE/Favi-BE.API
   k6 run --vus 30 --duration 60s k6-tests/load/scenario-2-feed-refresh-steady-users.js
   ```
2. Tại giây thứ 20 của kịch bản, cố tình đánh sập container `favi-api-1`:
   ```powershell
   docker stop favi-api-1
   ```
3. **Hiện tượng quan sát**:
   - **Tại k6 output**: Không phát sinh bất kỳ lỗi kết nối (HTTP 502/503) nào đối với client (`failed requests = 0%`), do NGINX tự động phát hiện lỗi và chuyển hướng (failover) ngay lập tức sang `favi-api-2`.
   - **Tại Grafana**: RPS duy trì liền mạch, không bị đứt gãy đồ thị.
4. Bật lại node bị sập:
   ```powershell
   docker start favi-api-1
   ```
   Sau khi container hoàn tất khởi động và vượt qua health check, NGINX tự động đưa node trở lại cụm `least_conn` cân bằng tải.

---

## 8. Tiêu Chuẩn Nghiệm Thu Hiệu Năng (SLA / SLO Targets)

Mọi kịch bản kiểm thử tải trước khi release phiên bản mới bắt buộc phải thỏa mãn các tiêu chuẩn định lượng sau:

| Danh Mục Đánh Giá | Ngưỡng Mục Tiêu (SLO) | Trạng Thái Không Đạt (Breach) |
| :--- | :--- | :--- |
| **Độ trễ đọc bảng tin (p95)** | `<= 250ms` | `> 500ms` |
| **Độ trễ đọc bảng tin (p99)** | `<= 500ms` | `> 1000ms` |
| **Độ trễ ghi dữ liệu (Post/React p95)**| `<= 350ms` | `> 800ms` |
| **Tỷ lệ lỗi (Error Rate)** | `< 0.1%` (dưới tải bình thường) | `> 1.0%` |
| **Chịu tải Failover** | `100% 200 OK` (0 request rớt khi 1 replica crash) | Xuất hiện HTTP 502/504 tới client |
| **Rò rỉ tài nguyên (Soak 2h)** | Mức RAM .NET và Redis phẳng, không tăng lũy tiến | RAM tăng liên tục không hồi phục sau GC |
