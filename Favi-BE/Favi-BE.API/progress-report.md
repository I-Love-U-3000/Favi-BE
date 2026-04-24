# Các seed và test trong này thì chuẩn, nhưng các kế hoạch triển khai nâng cấp đã lỗi thời, tôi đã có plan khác. 

#  Favi-BE 

## 1. Giới Thiệu Dự Án

Dự án Favi-BE là một hệ thống mạng xã hội backend được xây dựng bằng .NET 9, hiện tại đang ở dạng **monolith**. Mục tiêu chính hiện tại là hoàn thiện các tính năng cốt lõi, scale có chiến lược => Chuyển đổi dự án thành Modular monolith hoặc Microservice:
- **Xác định scope hợp lý** cho project để tập trung vào các tính năng cốt lõi và tránh lãng phí tài nguyên (ví dụ: giới hạn Cloudinary credits, seed data deterministic).
- **Thiết kế và implement seeding pipeline** để tạo dữ liệu test thực tế, đa dạng và reproducible (lặp lại được).
- **Tạo các test k6 toàn diện** (smoke, load, stress, functional, integration) để đảm bảo reliability (độ tin cậy) và performance (hiệu suất) của hệ thống.
- **Tách domain boundaries** thành modular monolith để giảm coupling (liên kết chặt chẽ) và chuẩn bị cho bước tiếp theo là microservice.
- **Tách project thành microservice** (các dịch vụ nhỏ, độc lập) để cải thiện scalability (khả năng mở rộng) và maintainability (dễ bảo trì).
- **Cải thiện performance và scaling** với chiến lược rõ ràng, giới hạn scope resource (ví dụ: Cloudinary 20 credits, seed data deterministic – dữ liệu seed không ngẫu nhiên).
- **Đảm bảo reliability** (độ tin cậy) thông qua testing pipeline toàn diện (k6 load/stress/functional/integration tests).

Dự án sử dụng kiến trúc **modular monolith** (monolith được chia module) làm bước chuyển tiếp trước khi tách thành microservice. Các module chính bao gồm: Auth (xác thực), Feed (bảng tin), Post (bài viết), Reaction (phản ứng), Comment (bình luận), Share (chia sẻ), Search (tìm kiếm), Notification (thông báo), Profile (hồ sơ).

## 2. Tiến Độ Đã Hoàn Thành

### 2.1 Scope Lại Project
- **Phân tích yêu cầu**: Xác định các domain boundaries (ranh giới miền) (Auth, Feed, Post, etc.) và dependencies (phụ thuộc) giữa chúng.
- **Giới hạn scope resource**: 
  - Sử dụng Cloudinary với 20 credits (chỉ upload media cần thiết, reuse seed data).
  - Seed data deterministic (không random) để test reproducible (lặp lại được).
  - Giới hạn test scenarios theo thực tế (không test mass create post vì hiếm xảy ra).
- **Resource allocation (Docker)**: 

  | Service          | CPUs | RAM    | Mục đích |
  |------------------|------|--------|----------|
  | favi-api        | 2    | 2.5GB  | API chính |
  | favi-postgres   | 2    | 2.5GB  | Database |
  | vector-index-api| 1.5  | 1GB    | Vector search |
  | qdrant          | 0.75 | 1GB    | Vector DB |
  | redis           | 0.5  | 512MB  | Cache/Health |

  (Source: `docker-compose.resource-allocation` – không đổi giữa benchmark runs để đảm bảo consistency – tính nhất quán).

### 2.2 Design và Implement Seeding Pipeline
- **Thiết kế pipeline**: 8 bước sequential deterministic (Users, Follows, Posts+Media, Engagement, Tags, Notifications, Auth Bootstrap, Export).
- **Entity mô phỏng thực tế**:
  - 5000 users với phân bố role (70% lurker – người xem, 25% casual – người dùng bình thường, 5% power users – người dùng năng động).
  - 11956 posts với media (image từ LoremFlickr deterministic).
  - 60616 follows (graph social network skewed – lệch, preferential attachment – ưu tiên kết nối với người nổi tiếng).
  - 115541 reactions (skewed: 80% posts <10 likes, 15% 10-50, 5% 50-150).
  - 22520 comments (tree depth 1-2, 25-35% replies, some with URLs).
  - 1772 reposts (unique per user-post pair).
  - 8020 notifications (generated from engagement).
  - Tags: 109 tags, mỗi post 1-3 tags, skewed popularity.
- **Validator layer**: SeedValidator quét toàn bộ data seeded để đảm bảo integrity (tính toàn vẹn) (unique constraints, FK, no orphan records, counts in range).
- **Output**: CSV files trong `seed-output/` cho k6 tests sử dụng (users.csv, posts.csv, tokens.csv, etc.).
- **Distribution models**: 
  - User roles: 90-9-1 skewed (lurker/casual/power).
  - Follow graph: Zipf-like heavy-tail (phân bố đuôi dài, ít người nổi tiếng).
  - Engagement: Zipf for post popularity (hot/cold posts).
  - Image: LoremFlickr URLs với lock để deterministic.

### 2.3 Tạo Các Test k6 Toàn Diện
- **6 bộ test** theo convention (common.js, scenario-*.js, run-all.ps1, README.md), dựa trên `detail-testing-plan/`:
  - **Smoke/Sanity** (2 scenarios): Basic endpoint checks (auth login, feed read, post create/delete, reaction like, comment create, profile privacy). Mô phỏng single user để verify hệ thống sống.
    - Modules tested: Auth, Feed, Post, Reaction, Comment, Profile.
    - Reference: `01-smoke-sanity.md`.
  - **Load** (7 scenarios): Steady users (feed refresh, fan-out worst-case, reaction mix, like/unlike rotating, trending post heavy read, profile scroll, search related). Đo P50/P95/P99 latency, throughput, error rate dưới tải bình thường.
    - Modules tested: Feed (fan-out on read), Reaction (idempotency), Post (heavy read), Profile, Search.
    - Reference: `04-load.md`.
  - **Stress/Spike/Soak** (12 scenarios): High load (mass login, continuous feed, hotspot reaction, noti storm, mass create, media partial failures, sudden spikes, viral post, long-run consistency). Phát hiện breaking points, recovery, memory leaks.
    - Modules tested: Auth (mass login), Feed (continuous), Reaction (hotspot), Notification (storm), Post (create), Media (failures), all for spike/soak.
    - Reference: `05-stress-spike-soak.md`.
  - **Functional** (6 scenarios): Single-module logic (auth/privacy, feed pagination/dedupe, post CRUD/media, reaction idempotency, search/related, notifications). Kiểm tra logic cục bộ đúng.
    - Modules tested: Auth (privacy), Feed (pagination), Post (CRUD), Reaction (idempotency), Search, Notification.
    - Reference: `02-functional.md`.
  - **Integration/E2E** (4 scenarios): Cross-module flows (user journeys, consistency checks, read-after-write, regression set). Xác nhận end-to-end và eventual consistency (tính nhất quán cuối cùng).
    - Modules tested: All (user journey, consistency, read-after-write).
    - Reference: `03-integration-e2e.md`.
- **Tổng cộng ~31 scenarios**, sử dụng seed data deterministic.
- **Thresholds**: P95/P99 latency, error rate, throughput để đo performance baseline.
- **Modules tested overall**: Auth (login/session/privacy), Feed (pagination/ranking/fan-out), Post/Media (CRUD/upload), Reaction/Comment/Share (idempotency/trees), Search (semantic/related), Notification (side-effects), Profile (privacy/follow).

## 3. Những Gì Chưa Làm

### 3.1 Tách Domain Boundaries Thành Modular Monolith
- **Hiện tại**: Code vẫn là monolith với shared DbContext và repositories.
- **Cần làm**: 
  - Tách thành modules riêng (AuthModule, FeedModule, etc.) với DbContext riêng hoặc shared nhưng isolated.
  - Implement interfaces/contracts giữa modules (ví dụ: Feed depends on Post/Reaction services).
  - Refactor controllers/services để giảm coupling (liên kết chặt chẽ).

### 3.2 Tối Ưu Code
- **Performance bottlenecks**: Feed scoring per request, N+1 queries, offset pagination degradation, race conditions in reactions.
- **Cần làm**:
  - Add Redis cache cho feed (key: `feed:{userId}:{page}`, TTL: 5min).
  - Fix N+1 queries, add indexes (CreatedAt, partial indexes).
  - Implement cursor pagination thay offset.
  - Handle race conditions (idempotent reaction toggle, catch 23505).

### 3.3 Triển Khai Thành Microservice
- **Sau modular monolith**: Tách thành services riêng (Auth Service, Feed Service, etc.).
- **Cần làm**:
  - Containerize với Docker (multi-service compose).
  - Implement API Gateway (YARP) route requests.
  - Add service discovery, circuit breaker (Polly).
  - Migrate DB: Shared DB -> per-service DB với event-driven sync (outbox pattern).
  - Handle distributed transactions (Saga pattern nếu cần).

### 3.4 Tạo Bản k6 Test Mới
- **Sau microservice**: Sửa URL test cũ (ví dụ: `api/posts` -> `feed-service/api/posts`).
- **Cần làm**:
  - Update common.js với base URLs mới.
  - Add tests cho inter-service calls (gateway routing).
  - Test distributed consistency (eventual consistency across services).

## 4. Sơ Đồ Dependency Hiện Tại

Dưới đây là sơ đồ dependency của hệ thống hiện tại (monolith). Sử dụng text-based diagram.

```
thiếu
```

**Giải thích sơ đồ**:
- **Controllers**: Entry points, thin layer.
- **Services**: Business logic, orchestrate repositories.
- **Repositories**: Data access, EF Core.
- **Dependencies**: Arrows chỉ ra "depends on" (service gọi service khác).
- **External**: Cloudinary, Qdrant, Redis.
- **Shared**: DbContext là điểm coupling chính (cần tách trong microservice).

## 5. Kế Hoạch Triển Khai Tiếp Theo

### 5.1 Giai Đoạn 1: Modular Monolith (2-3 tuần)
1. **Tách modules**:
   - Tạo folders: `Modules/Auth`, `Modules/Feed`, etc.
   - Implement interfaces: `IPostService`, `IFeedService`.
   - Refactor DbContext: Shared nhưng với scoped queries.
2. **Test**: Chạy lại functional/integration tests để đảm bảo không break.
3. **Optimize**:
   - Add Redis cache cho feed (TTL-based, invalidate on write).
   - Fix N+1 queries, add indexes.
   - Implement idempotent reaction (catch 23505, return success).

### 5.2 Giai Đoạn 2: Microservice Migration (3-4 tuần)
1. **Containerize**:
   - Docker compose với services: auth-service, feed-service, post-service, etc.
   - API Gateway (YARP) route requests.
2. **DB Migration**:
   - Per-service DB (auth-db, feed-db, etc.).
   - Event-driven sync: Post created -> publish event -> Feed service consume.
3. **Reliability**:
   - Circuit breaker cho external calls (Cloudinary, Qdrant).
   - Health checks, logging (Serilog).
4. **Test**: Update k6 tests với URLs mới, add inter-service tests.

### 5.3 Giai Đoạn 3: Performance Tuning & Scaling (2 tuần)
1. **Benchmark**: Chạy k6 load/stress, đo before/after.
2. **Scaling**:
   - Horizontal scaling với replicas.
   - Load balancer (nginx).
3. **Monitoring**: Add metrics (Prometheus), alerts.

### 5.4 Risk & Milestones
- **Risk**: Resource limits (Cloudinary credits), time constraints.
- **Milestones**:
  - Tuần 1-2: Modular monolith done.
  - Tuần 3-5: Microservice basic.
  - Tuần 6-7: Optimized & tested.

## 6. Glossary (Thuật Ngữ Giải Thích)

- **Monolith**: Ứng dụng một khối duy nhất, tất cả code trong một project.
- **Modular Monolith**: Monolith được chia thành modules riêng nhưng vẫn chạy cùng process.
- **Microservice**: Các dịch vụ nhỏ, độc lập, giao tiếp qua network.
- **Fan-out on read**: Khi đọc feed, assemble từ nhiều nguồn (posts, reactions) thay vì pre-compute.
- **Idempotent**: Thao tác gọi nhiều lần vẫn cho kết quả giống nhau, không lỗi.
- **Eventual Consistency**: Dữ liệu nhất quán cuối cùng, không ngay lập tức.
- **Zipf Distribution**: Phân bố lệch, ít item phổ biến, nhiều item hiếm.
- **k6**: Công cụ load testing, đo latency/throughput.
- **EF Core**: Entity Framework Core, ORM cho .NET.
- **DbContext**: Context quản lý DB connections và queries trong EF Core.

## 7. Kết Luận

Đã hoàn thành **foundation** (scope, seeding, testing pipeline). Tiếp theo tập trung **refactor code** và **migration**. Báo cáo này sẽ được update hàng tuần để theo dõi tiến độ.

**Câu hỏi cho cô**: [Nếu có, liệt kê ở đây]

---

**Phụ lục**: 
- Link GitHub: https://github.com/I-Love-U-3000/Favi-BE
- Seed output: `Favi-BE.API/seed-output/`
- Test results: `Favi-BE.API/k6-tests/*/test-results/`
- Seeding plan: `detail-enough-infomation-seeding-plan.md`
- Testing plans: `detail-testing-plan/00-overview-and-structure.md`, `01-smoke-sanity.md`, `02-functional.md`, `03-integration-e2e.md`, `04-load.md`, `05-stress-spike-soak.md`
