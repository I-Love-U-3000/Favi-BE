# 05 — Horizontal Scaling, Performance & Observability (Favi-BE)

## 1. Horizontal Scaling Strategy

Favi-BE is engineered around **Stateless Application** principles:
- **No In-Process State Persistence**: User sessions, authentication tokens (JWT), and idempotency locks are distributed on Redis or encrypted within client tokens.
- **Seamless Replica Scaling**:
  In `docker-compose.yml`, the system runs 2 API replicas by default (`favi-api-1` and `favi-api-2`). To scale out under higher load:
  1. Spin up additional containers: `favi-api-3:8080`, `favi-api-4:8080`.
  2. Declare new upstream nodes in `favi_api_cluster` and `favi_signalr_cluster` within `gateway/conf.d/gateway.conf`.
  3. Reload NGINX without downtime (`nginx -s reload`).

---

## 2. Read/Write Segregation (CQRS Performance)

To achieve high throughput on read-heavy workflows (which represent 90% of social media traffic, such as feed scrolling and profile viewing), the system segregates read and write paths:

```
[ Command (Write) ] ──▶ EF Core ChangeTracker ──▶ Master PostgreSQL (Write)
                                                       │
[ Query (Read) ]    ──▶ EF Core .AsNoTracking() ───────┘ (Current)
                        │
                        ▼ (Roadmap for Hot Paths)
                        Dapper Projections / Read Replicas (Future)
```

1. **Write Path**: EF Core tracking context enforcing full domain aggregate relationships, validation rules, and consistency invariants.
2. **Read Path**: Mandatory `.AsNoTracking()` across all Query Handlers to:
   - Eliminate EF Core `ChangeTracker` snapshot overhead.
   - Drastically reduce Garbage Collection (GC) heap memory allocations.
   - Query strictly via `.Select(x => new Dto { ... })` projection to fetch only necessary columns from the database.
3. **Multi-Tier Caching with Redis**:
   - Cache user profile snapshots (`favi:profile:{id}`).
   - Cache trending tags and public collections.
   - Proactive cache invalidation triggered immediately upon receiving corresponding domain/integration events.

---

## 3. PostgreSQL Query & Processing Optimization

To support millions of interactions without database I/O bottlenecks, Favi-BE enforces the following data-tier optimization practices:

### 3.1 PostgreSQL Indexing Strategy
1. **Composite Indexes**:
   - Configured on tables with the highest read query traffic according to filter and sorting predicates:
     - `Posts` table: Index `("UserId", "CreatedAt" DESC)` optimizes user profile timeline queries.
     - `Comments` table: Index `("PostId", "CreatedAt" ASC)` and `("ParentCommentId")` enables fast hierarchical comment tree assembly without full table scans.
     - `Follows` table: Unique composite index `("FollowerId", "FolloweeId")` guarantees relationship uniqueness while accelerating follow status lookups between users.
2. **Foreign Key Indexes**:
   - PostgreSQL does not automatically index foreign key columns. All relational foreign keys (`AuthorId`, `PostId`, `ConversationId`, `CollectionId`) are explicitly assigned B-Tree indexes to avoid table-level locks and full table scans during JOIN operations or cascading deletes.
3. **Partial Indexes**:
   - Minimize index footprint and write amplification by indexing only active, business-relevant subsets:
     - `Stories` table: `CREATE INDEX idx_stories_active ON "Stories" ("CreatedAt") WHERE "ExpiresAt" > NOW();`
     - Soft-delete tables: `CREATE INDEX ... WHERE "IsDeleted" = false;`
4. **Specialized Indexes & Offloading (GIN / Trigram / Vector)**:
   - For fuzzy text search across usernames and tags, use `pg_trgm` GIN indexes.
   - For image and semantic multimodal search, heavy vector cosine distance computations are completely offloaded to the standalone `vector-index-api` (FastAPI) and Qdrant cluster to protect PostgreSQL CPU resources.

### 3.2 High-Performance Pagination: Keyset / Cursor-Based Pagination
- **The Offset/Limit Problem (`Skip().Take()`)**: As users scroll deeper into feeds (e.g., page 1,000, translating to `OFFSET 20000 LIMIT 20`), the database must still scan and discard 20,000 index entries before returning the 20 requested rows ($O(N)$ degradation with scroll depth).
- **Keyset Pagination Solution (Cursor)**:
  - All feed and list endpoints (`/api/posts/feed`, `/api/posts/user/{id}`, `/api/comments`) employ a composite timestamp + primary key cursor:
    ```sql
    SELECT * FROM "Posts"
    WHERE ("CreatedAt", "Id") < (@cursorCreatedAt, @cursorId)
    ORDER BY "CreatedAt" DESC, "Id" DESC
    LIMIT 20;
    ```
  - This enables the database engine to navigate directly to the target B-Tree leaf node in constant $O(1)$ time, maintaining uniform low latency regardless of feed depth.

### 3.3 Mitigating N+1 Queries & Cartesian Product Explosion
1. **Explicit Projections**:
   - Query Handlers must use `.Select(x => new PostDto { ... })` to pull only required attributes, avoiding eager loading of unused relational entities.
2. **Split Queries for Multi-Level Relations**:
   - When loading 1-to-N relationships (e.g., a Post with attached Media and Reactions), apply `.AsSplitQuery()`:
     ```csharp
     var post = await _context.Posts
         .AsNoTracking()
         .AsSplitQuery()
         .Include(p => p.Media)
         .Include(p => p.Tags)
         .FirstOrDefaultAsync(p => p.Id == postId, cancellationToken);
     ```
   - EF Core breaks the query into discrete SQL queries dispatched in batch, eliminating giant Cartesian product duplications across network and memory buffers.

### 3.4 Batching & Bulk Operations
1. **Batch Chunking in Seed & Synchronization**:
   - When inserting or synchronizing thousands of records (as in the Deterministic Seed Pipeline), data is partitioned into chunks of **500 - 1,000 records**, calling `AddRangeAsync()` followed by a single `SaveChangesAsync()` per chunk rather than invoking commits inside iterative loops.
2. **Direct Bulk Update / Delete**:
   - For mass administrative status transitions or cleanup (e.g., marking notifications read, archiving expired stories), leverage `ExecuteUpdateAsync()` and `ExecuteDeleteAsync()` (EF Core 7+) to emit single direct SQL statements without loading entities into memory.

### 3.5 Slow Query Profiling & EXPLAIN ANALYZE
1. **Query Inspection Standard**:
   - Developers must validate complex queries in staging environments using `EXPLAIN (ANALYZE, BUFFERS)`:
     - Goal: Eliminate all `Seq Scan` (Sequential Scans) on tables exceeding 10,000 rows.
     - Verify filter operations resolve to `Index Scan` or `Bitmap Index Scan`.
2. **Production Slow Query Alerts**:
   - PostgreSQL is configured to log queries exceeding acceptable execution thresholds:
     ```
     log_min_duration_statement = 200 # Log all queries executing longer than 200ms
     ```
   - These logs are continuously parsed by log collectors to trigger automated engineering alerts.

---

## 4. PostgreSQL High-Load Configuration Tuning

In `docker-compose.yml`, the `favi-postgres` service is tuned specifically for high-concurrency social media workloads:

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

- **`max_connections=250`**: Accommodates multiple API replica connection pools without connection exhaustion (`Maximum Pool Size=100;`).
- **`shared_buffers=512MB`**: Increases Postgres buffer cache in RAM for frequently accessed pages.
- **`work_mem=16MB`**: Accelerates complex in-memory sorting (`ORDER BY`), grouping (`GROUP BY`), and hash joins (`HASH JOIN`) for feed queries.

---

## 5. Observability Stack & Health Monitoring

The system includes a pre-configured observability pipeline for real-time performance tracking:

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

### 5.1 Metrics Collection via Prometheus
- **NGINX Status Endpoint**: Internal `/nginx_status` scraped by `favi-nginx-exporter`.
- **Key Metrics Tracked**:
  - `nginx_connections_active`: Current open real-time client connections.
  - `nginx_http_requests_total`: Total HTTP requests partitioned by status code (2xx, 4xx, 5xx).
  - Rate-limit rejection rate (HTTP 429 events).
  - Upstream backend replica health and latency (`up_status`, `up_time`).

### 5.2 Grafana Dashboards
- Accessible at: `http://localhost:3050` (Default credentials: `admin` / `admin`).
- Pre-built dashboard visualizations:
  - Throughput (RPS - Requests Per Second).
  - Error budget breakdown (5xx vs 2xx ratios).
  - Latency percentiles (Response Latency p50, p95, p99).
  - CPU and RAM utilization across all containerized workloads.

### 5.3 Health Check Endpoints
- URL: `GET /health` (bypasses authentication and rate limiting).
- Verifies connectivity to: PostgreSQL primary, Redis cluster, and the Vector Index API.
- Leveraged by Docker Compose health checks to determine replica readiness before directing live ingress traffic.
