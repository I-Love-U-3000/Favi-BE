# Microservice Migration Blueprint (from current Favi-BE monolith)

## 0) Mục tiêu và phạm vi

Tài liệu này mô tả lộ trình chuyển đổi hệ thống hiện tại (ASP.NET Core `.NET 9`, monolith, `AppDbContext` dùng chung) sang kiến trúc microservice theo sơ đồ mục tiêu:

- `API Gateway (route by intent)`
- `Read API service`
- `Write API service`
- `Fan-out worker`
- `Message bus`
- `PostgreSQL primary + read replica`
- `Redis`
- `Qdrant`

Mục tiêu kỹ thuật:
1. Không vỡ nghiệp vụ hiện có.
2. Cải thiện hiệu năng read-path (feed/search/profile GET).
3. Giảm coupling write-path.
4. Tạo nền tảng scale theo chiều ngang.
5. Giữ benchmark comparability (same seed, same resource contract, same k6 pattern before/after).

---

## 1) Hiện trạng codebase (as-is)

### 1.1 Entry points chính
Từ code hiện tại, nghiệp vụ social chính đi qua các controller:

- `AuthController` (`/api/auth/*`)
- `PostsController` (`/api/posts/*`)
- `CommentsController` (`/api/comments/*`)
- `ProfilesController` (`/api/profiles/*`)
- `SearchController` (`/api/search/*`)
- `NotificationsController` (`/api/notifications/*`)

### 1.2 Data & infra hiện tại
- 1 `AppDbContext` dùng chung.
- Repositories tập trung qua `UnitOfWork`.
- PostgreSQL là store chính.
- Redis hiện thiên về health/observability (chưa là business cache đầy đủ cho feed).
- Vector search dùng service + Qdrant, có flow async side-effect.

### 1.3 Rủi ro đã thấy từ test
- Race condition write-path (ví dụ duplicate key `IX_Reactions_PostId_ProfileId` dưới concurrent toggle).
- Feed read path fan-out on read dễ bottleneck khi tải tăng.
- Offset pagination có nguy cơ kém ổn định dưới write liên tục.

---

## 2) Kiến trúc đích (to-be) theo diagram

## 2.1 Service decomposition

### A. API Gateway
- Vai trò: single public entry, auth propagation, rate limit, route by intent.
- Gateway: `YARP` hoặc `Nginx` 

### B. Write API Service
**Ownership**: command-side nghiệp vụ ghi.
- `Auth`: login/register/refresh.
- `Post`: create/update/delete/archive/restore.
- `Reaction`: toggle.
- `Comment`: create/update/delete.
- `Follow`: follow/unfollow.
- `Share/Repost`.
- `Media upload`.

Scale target: IOPS + transaction throughput.

### C. Read API Service
**Ownership**: query-side nghiệp vụ đọc.
- Feed (`/feed`, `/guest-feed`, `/latest`, `/explore`, `/feed-with-reposts`).
- Profile GET/read models.
- Post detail read model.
- Search proxy + related query path.

Scale target: RAM/cache hit ratio + read QPS.

### D. Fan-out Worker
**Ownership**: async side-effects + projection updates.
- Notification fan-out.
- Search indexing events.
- Counter/cache projection updates.
- Replay/rebuild read models.

Scale target: CPU + queue throughput + consumer lag.

## 2.2 Data planes
- `PostgreSQL primary`: write source of truth.
- `PostgreSQL read replica`: read service.
- `Redis`: hot cache + counters + optional rate limit state.
- `Qdrant`: vector index/query.

## 2.3 Messaging
- `Kafka` hoặc `Redis Streams`.
- Bắt buộc dùng `Outbox pattern` tại write side để không mất event.
- Consumer dùng idempotent handling (`Inbox`/dedupe key).

---

## 3) Domain boundaries & ownership map

## 3.1 Bounded contexts đề xuất
1. `Identity` (Auth + account lifecycle)
2. `SocialGraph` (follow/follower)
3. `Content` (post/media)
4. `Engagement` (reaction/comment/share)
5. `FeedQuery` (read-model + ranking query)
6. `Notification` (fan-out)
7. `Search` (semantic + related orchestration)

## 3.2 Ownership principle
- Dữ liệu ghi thuộc service nào thì service đó là owner duy nhất của table write.
- Service khác đọc qua:
  - API contract hoặc
  - read model/projection được đồng bộ qua event.
- Tránh truy cập chéo trực tiếp DB của service khác.

---

## 4) API contract strategy

## 4.1 External API compatibility
- Giữ backward compatibility cho client hiện tại bằng gateway mapping.
- Giai đoạn chuyển tiếp: giữ path công khai `/api/...` như cũ, route nội bộ sang service tương ứng.

## 4.2 Route-by-intent (internal)
- Read routes -> Read service
- Write routes -> Write service
- Background event routes/internal -> Worker

## 4.3 Versioning
- Bắt đầu chuẩn hoá event/API version ngay từ phase modular monolith (`v1` suffix trong event name).

---

## 5) Event catalog tối thiểu (v1)

Producer (Write service) -> Bus -> Consumers (Worker/Read):

1. `PostCreated.v1`
2. `PostUpdated.v1`
3. `PostArchived.v1`
4. `PostDeleted.v1`
5. `ReactionToggled.v1`
6. `CommentCreated.v1`
7. `CommentDeleted.v1`
8. `FollowCreated.v1`
9. `FollowRemoved.v1`
10. `RepostCreated.v1`
11. `RepostRemoved.v1`

Mỗi event payload tối thiểu có:
- `eventId` (GUID)
- `eventType`
- `occurredAt`
- `aggregateId`
- `actorId`
- `version`
- business payload

---

## 6) Migration phases (khuyến nghị thực thi)

## Phase 0 — Stabilize current monolith (bắt buộc trước khi tách)
- Fix race condition critical path (reaction toggle idempotent-safe).
- Chuẩn hoá error mapping (không ném raw DB exception ra ngoài).
- Freeze benchmark baseline (seed + resource allocation + test matrix).

Exit criteria:
- K6 `sanity/load` không crash do known race bug.
- Baseline report before-split có số liệu ổn định.

## Phase 1 — Modular monolith hard boundaries
- Tách project theo module trong cùng solution/process.
- Mỗi module có application layer + domain contracts rõ.
- Cắt direct dependency vòng tròn giữa services.

Exit criteria:
- `Auth/Content/Engagement/FeedQuery/Notification/Search` có boundary rõ.
- Unit + functional + integration pass.

## Phase 2 — Introduce outbox + message bus while still monolith
- Viết outbox table và publisher worker.
- Consumer prototype cho notification/search projection.
- Bắt đầu materialized read models.

Exit criteria:
- Event phát ổn định, có retry, có dead-letter handling.
- Replay được event để rebuild projection.

## Phase 3 — Extract Fan-out Worker (service đầu tiên)
- Chuyển logic async side effects ra service riêng.
- Monolith gọi bus thay vì gọi trực tiếp side-effects.

Exit criteria:
- Notification/indexing chạy qua bus 100%.
- Không mất event trong test spike/hotspot.

## Phase 4 — Extract Read API Service
- Read endpoints chuyển sang service riêng + read replica + Redis cache.
- Gateway route đọc sang service mới.

Exit criteria:
- Read service phục vụ đầy đủ feed/profile/detail/search read routes.
- P95 feed cải thiện so với baseline trước extract.

## Phase 5 — Extract Write API Service
- Write endpoints tách khỏi monolith cũ.
- Monolith legacy giữ vai trò fallback hoặc bị rút gọn.

Exit criteria:
- Write commands chạy độc lập + publish events ổn.
- Integration/E2E pass qua gateway.

## Phase 6 — Scale & hardening
- Horizontal scaling theo role:
  - Read service scale theo QPS
  - Write service scale theo txn throughput
  - Worker scale theo queue lag
- Tinh chỉnh autoscaling + rate limiting.

Exit criteria:
- Stress/Spike/Soak đạt SLA mục tiêu đã chốt.

---

## 7) Resource strategy (benchmark contract)

Giữ cố định contract tài nguyên cho before/after:
- `favi-api`: 2 CPU / 2.5GB
- `favi-postgres`: 2 CPU / 2.5GB
- `vector-index-api`: 1.5 CPU / 1GB
- `qdrant`: 0.75 CPU / 1GB
- `redis`: 0.5 CPU / 512MB

Nguồn chuẩn: `docker-compose.resource-allocation`.
Không đổi contract khi so sánh performance giữa các phase.

---

## 8) Testing migration strategy (k6)

## 8.1 Nguyên tắc
- Không bỏ bộ test cũ.
- Clone test suites thành phiên bản `gateway-aware` để chỉ đổi base route mapping.
- Giữ cùng dataset seed + same scenario intensity để so before/after.

## 8.2 Mapping test suites
- `smoke/sanity`: gate nhanh sau mỗi refactor/extract.
- `functional`: validate module behavior per service boundary.
- `integration-e2e`: validate cross-service correctness.
- `load`: baseline/steady throughput.
- `stress-spike-soak`: validate failure behavior + recovery.

## 8.3 Additional checks sau tách
- Queue lag SLA
- Event delivery success ratio
- Cache hit ratio read service
- Replica lag impact to read-after-write SLAs

---

## 9) Observability & SRE minimum

Bắt buộc trước khi production-like test:
1. Correlation ID xuyên gateway -> services -> workers.
2. Structured logs (JSON).
3. Metrics:
   - latency p50/p95/p99 theo endpoint
   - error rate 4xx/5xx
   - throughput req/s
   - DB pool usage
   - queue lag
4. Distributed tracing (OpenTelemetry).
5. Alert rules (5xx spike, queue lag high, replica lag high).

---

## 10) Rủi ro chính và mitigation

1. **Data inconsistency khi split DB**
   - Mitigation: outbox + idempotent consumer + replay tooling.

2. **Performance regressions do network hops**
   - Mitigation: gateway cache policy + read model + connection pooling.

3. **Contract drift giữa services**
   - Mitigation: OpenAPI contract tests + consumer-driven checks.

4. **Deployment complexity tăng mạnh**
   - Mitigation: tách theo phase, không big-bang rewrite.

---

## 11) Deliverables theo từng phase (để báo cáo giảng viên)

- Phase 0: Baseline benchmark report + known bottleneck list.
- Phase 1: Modular boundary map + dependency cleanup report.
- Phase 2: Event catalog + outbox/inbox implementation doc.
- Phase 3: Fan-out worker runbook + reliability metrics.
- Phase 4: Read service benchmark deltas (before/after).
- Phase 5: Full gateway E2E pass matrix.
- Phase 6: Scale report + capacity recommendation.

---

## 12) Kết luận thực thi

Lộ trình khuyến nghị cho codebase hiện tại là:
`Monolith -> Modular monolith -> Event backbone -> Fan-out extraction -> Read extraction -> Write extraction -> Scale hardening`.

Tránh triển khai microservice theo kiểu cắt thẳng toàn hệ thống trong một lần.
