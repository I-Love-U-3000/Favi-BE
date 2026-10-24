# 07 — Testing, Performance & Benchmarking Strategy (Favi-BE)

This document specifies the comprehensive multi-tier testing strategy for the **Favi Backend (Favi-BE)** platform: spanning architecture verification tests (`ArchitectureTests`), functional suites (Smoke, Functional, Sanity), to high-throughput load, stress, spike, and soak testing executed via **k6**, correlated in real time with **Prometheus & Grafana**.

---

## 1. Testing Pyramid & Quality Gates

The system adheres to a disciplined hierarchical testing pyramid designed to detect regressions at the lowest possible layer before promoting artifacts to load environments:

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

### Quality Gate Hierarchy:
1. **Gate 1 (Build / Local Development)**: All Unit Tests and Architecture Tests must pass 100%. Any violation of module encapsulation or data exposure through Privacy Guard fails the build immediately.
2. **Gate 2 (Smoke CI Gate)**: The automated k6 Smoke suite executes under 2 minutes using 1–2 VUs, verifying all essential HTTP routes respond 200 OK.
3. **Gate 3 (Load & Resilience Gate)**: Executes k6 Load and Gateway suites through the NGINX reverse proxy (`http://localhost:5000`), verifying balanced load distribution between `favi-api-1` and `favi-api-2` with p95 latency < 250ms and error rates < 0.1%.

---

## 2. Architecture Tests & Module Boundary Enforcement (`Favi-BE.ArchitectureTests`)

Automated using `NetArchTest.Rules` and `xUnit` in project [`Favi-BE.ArchitectureTests`](../Favi-BE/Favi-BE.ArchitectureTests) to strictly enforce structural software design rules:

### 2.1 Enforced Architectural Invariants
- **Module Isolation**: Source code within a Bounded Context (e.g., `ContentDiscovery`) **must never reference** internal namespaces or DB Contexts of sibling modules (e.g., `Engagement` or `SocialGraph`).
- **Clean Architecture Dependency Inversion**: The `Domain` layer must not depend on `Application`, `Infrastructure`, or third-party packages (outside of `BuildingBlocks.Domain`).
- **Query Immutability**: All Query Handlers are strictly read-only (`.AsNoTracking()`), and are forbidden from calling `SaveChanges()` or altering entity tracking states.
- **Privacy Guard Enforcement ([`PrivacyGuardTests.cs`](../Favi-BE/Favi-BE.ArchitectureTests/PrivacyGuardTests.cs))**: Guarantees sensitive attributes (emails, password hashes, private post payloads) are never exposed via public-facing DTO contracts.

### 2.2 Execution Command
```powershell
dotnet test Favi-BE/Favi-BE.ArchitectureTests/Favi-BE.ArchitectureTests.csproj --logger "console;verbosity=normal"
```

---

## 3. Unit Tests (`Favi-BE.UnitTests`)

Located at [`Favi-BE.UnitTests`](../Favi-BE/Favi-BE.UnitTests), focusing on pure business logic validation in total isolation from databases or networks:
- Validation of Domain Entities and Value Objects.
- Enforcement of Business Invariants (e.g., cannot submit duplicate reaction types without first toggling off the previous reaction).
- Pagination calculation math and domain event emission logic.

### Execution Command
```powershell
dotnet test Favi-BE/Favi-BE.UnitTests/Favi-BE.UnitTests.csproj
```

---

## 4. k6 Load & Performance Testing Suite (`Favi-BE.API/k6-tests/`)

Favi-BE bundles a comprehensive suite of k6 load scenarios in [`Favi-BE/Favi-BE.API/k6-tests/`](../Favi-BE/Favi-BE.API/k6-tests), organized into distinct execution categories:

### 4.1 Scenario Catalog Overview

| Category | Directory / File | Scenario Count | Primary Objective |
| :--- | :--- | :---: | :--- |
| **Gateway & LB** | `k6-tests/gateway-loadbalancer-test.js` | 1 | Validates `least_conn` routing, Keep-Alive connection pooling, and HTTP 429 Rate Limiter behavior. |
| **Smoke** | `k6-tests/smoke/` | 13 | Fast verification across 13 core modules: Auth, Feed, Reaction, Comment, Post CRUD, Stories, Chat, Follow... |
| **Sanity** | `k6-tests/sanity/` | 8 | Validates data consistency and side-effect pipelines following seed runs. |
| **Functional** | `k6-tests/functional/` | 7 | Verifies complex end-to-end workflows: privacy restrictions, bidirectional chat, and notification fanout. |
| **Load** | `k6-tests/load/` | 8 | Measures throughput, p95/p99 latency against defined SLAs/SLOs under sustained concurrency. |
| **Stress, Spike & Soak** | `k6-tests/stress-spike-soak/` | 12 | Tests system limits: Stress (breakpoint discovery), Spike (sudden load surges), Soak (2h+ stability & memory leak detection). |

---

### 4.2 Load Testing Scenarios (`k6-tests/load/`)

1. **`scenario-1-auth-login-bulk-users.js`**: Concurrent logins across pre-seeded user accounts.
2. **`scenario-2-feed-refresh-steady-users.js`**: Sustained scrolling and feed refreshing (`/api/posts/feed-with-reposts`).
3. **`scenario-2-1-feed-fanout-worstcase-power-users.js`**: Feed fanout load against "Power Users" following thousands of accounts.
4. **`scenario-3-reaction-steady-mix.js`**: Concurrent reaction submissions (Like, Love, Haha, Wow, Sad, Angry) targeting posts.
5. **`scenario-4-like-unlike-rotating-users.js`**: Rapid reaction toggling to stress-test race condition handling and Redis counter sync.
6. **`scenario-5-trending-post-heavy-read.js`**: Read-heavy traffic concentrated on top trending content.
7. **`scenario-6-profile-scroll-steady.js`**: Navigating user profiles, post grids, and social connections.
8. **`scenario-7-search-related-moderate.js`**: Full-text and keyword search with related post discovery queries.

---

### 4.3 Stress, Spike & Soak Scenarios (`k6-tests/stress-spike-soak/`)

#### A. Stress (Finding System Breakpoints)
- `scenario-a1-stress-auth-login-mass-users.js`: Overloads authentication endpoints to verify NGINX `auth_limit` burst defenses.
- `scenario-a2-stress-feed-refresh-continuous-mass.js`: Incrementally ramps VUs until latency degrades beyond 1,000ms.
- `scenario-a3-stress-reaction-hotspot-mass-users.js`: Funnels high-concurrency reaction mutations into a single hotspot post.
- `scenario-a4-stress-noti-storm-hot-entity.js`: Notification storms targeting a single aggregate entity.
- `scenario-a5-stress-write-path-mass-create.js`: Stresses write-path throughput through the Transactional Outbox.
- `scenario-a6-stress-media-pipeline-partial-failures.js`: Simulates media upload load under intermittent network failures.

#### B. Spike (Sudden Load Surges)
- `scenario-b1-spike-feed-traffic-sudden-jump.js`: Sudden jump from 5 VUs to 150 VUs within 5 seconds.
- `scenario-b2-spike-login-after-event.js`: Simulates mass authentication surges following broadcast live events.
- `scenario-b3-spike-trending-post-viral.js`: Simulates immediate viral spikes on hot posts.

#### C. Soak (Long-Duration Stability)
- `scenario-c1-soak-feed-reaction-mix-long-run.js`: Continuous read/write traffic over 1–2 hours to identify memory leaks in .NET GC or Redis connection pools.
- `scenario-c2-soak-refresh-pattern-long-run.js`: Extended feed refresh cycling.
- `scenario-c3-soak-consistency-long-run.js`: Measures eventual consistency replication latency between Write DB and Read Projections over time.

---

## 5. Test Execution Guide

### 5.1 Prerequisites
1. **Active Docker Cluster**:
   ```powershell
   docker-compose up -d
   ```
2. **k6 CLI Installed**:
   ```powershell
   winget install GrafanaLabs.k6
   # Verify version:
   k6 version
   ```

---

### 5.2 Running Gateway & Load Balancing Tests
Validates L7 load balancing via the Gateway (`http://localhost:5000`), verifying Keep-Alive reuse and Rate Limiter triggers:

```powershell
cd Favi-BE/Favi-BE.API
k6 run k6-tests/gateway-loadbalancer-test.js
```

---

### 5.3 Automated PowerShell Test Runners

All test suites include an automated PowerShell runner (`run-all.ps1`):

#### 1. Run Smoke Tests (Fast end-to-end sanity check):
```powershell
cd Favi-BE/Favi-BE.API
pwsh ./k6-tests/smoke/run-all.ps1 -BaseUrl http://localhost:5000 -ShowSummary
```

#### 2. Run Load Tests (Quantitative SLA/SLO verification):
```powershell
cd Favi-BE/Favi-BE.API
pwsh ./k6-tests/load/run-all.ps1 -BaseUrl http://localhost:5000 -ShowSummary
```

#### 3. Run Stress & Spike Tests:
```powershell
cd Favi-BE/Favi-BE.API
pwsh ./k6-tests/stress-spike-soak/run-all.ps1 -BaseUrl http://localhost:5000 -ShowSummary
```

#### 4. Run 2-Hour Soak Test:
```powershell
cd Favi-BE/Favi-BE.API
pwsh ./k6-tests/stress-spike-soak/run-all.ps1 -BaseUrl http://localhost:5000 -SoakDuration 2h
```

---

### 5.4 Standalone Quick-Run Scripts

To execute standalone performance scripts located at the backend repository root:

```powershell
cd Favi-BE
# Test personalized feed reading with reposts (requires auth token):
k6 run load_test_feed.js

# Test breakpoint on guest feed browsing:
k6 run load_test_feed_guest_breakpoint.js

# Test breakpoint on authenticated feed browsing:
k6 run load_test_feed_auth_breakpoint.js

# Test login authentication throughput:
k6 run load_test_login.js
```

---

## 6. Live Observability Correlation with Prometheus & Grafana

During k6 test execution, engineers monitor live cluster telemetry:

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

### 6.1 Grafana Monitoring Dashboard
- **Access URL**: `http://localhost:3050` (Credentials: `admin` / `admin`).
- **Target Dashboard**: **Favi Cluster Performance & Gateway Overview** (`/d/favi-overview`).
- **Inspection Window**: Set time range to **Last 5 minutes**, auto-refresh rate to **5s**.

### 6.2 Key Monitored Telemetry Metrics

| Grafana Visual | Prometheus Metric | Technical Interpretation |
| :--- | :--- | :--- |
| **Request Throughput (RPS)** | `rate(nginx_http_requests_total[10s])` | Real-time requests processed per second by the Gateway. |
| **Active Client Connections** | `nginx_connections_active` | Total open client TCP sockets actively handled by the proxy. |
| **Waiting (Keep-Alive)** | `nginx_connections_waiting` | Idle Keep-Alive connections, demonstrating connection pool reuse. |
| **Rate Limit 429 Hits** | Rate of HTTP 429 responses | Confirms aggressive abuse attempts are blocked at the Gateway layer without reaching API pods. |

---

## 7. Zero-Downtime Failover & High Availability Validation

Validates the `proxy_next_upstream` directive configured in `gateway/conf.d/gateway.conf`:

```nginx
proxy_next_upstream error timeout invalid_header http_502 http_503 http_504;
proxy_next_upstream_tries 2;
```

### Chaos Failover Drill Procedure:
1. Start sustained k6 load over 60 seconds:
   ```powershell
   cd Favi-BE/Favi-BE.API
   k6 run --vus 30 --duration 60s k6-tests/load/scenario-2-feed-refresh-steady-users.js
   ```
2. At second 20 of the run, forcefully terminate `favi-api-1`:
   ```powershell
   docker stop favi-api-1
   ```
3. **Observed System Reaction**:
   - **k6 Output**: Zero dropped HTTP connections (`failed requests = 0%`), as NGINX detects the failed node and routes immediately to `favi-api-2`.
   - **Grafana Graph**: Cluster RPS remains continuous with zero drop-outs.
4. Restore the downed node:
   ```powershell
   docker start favi-api-1
   ```
   Once the container passes health checks, NGINX automatically reincorporates it into the active `least_conn` pool.

---

## 8. Performance SLA / SLO Acceptance Criteria

All release candidates must satisfy the following quantitative service level objectives prior to deployment:

| Criterion | Target Threshold (SLO) | Breach / Rejection Threshold |
| :--- | :--- | :--- |
| **Feed Read Latency (p95)** | `<= 250ms` | `> 500ms` |
| **Feed Read Latency (p99)** | `<= 500ms` | `> 1000ms` |
| **Write Mutation Latency (Post/React p95)**| `<= 350ms` | `> 800ms` |
| **Error Rate** | `< 0.1%` (under standard SLA load) | `> 1.0%` |
| **Failover Resilience** | `100% 200 OK` (0 client errors during replica drop) | Any HTTP 502/504 returned to client |
| **Resource Stability (2h Soak)** | Flat RAM and Redis footprint after GC | Unbounded memory growth without GC reclamation |
