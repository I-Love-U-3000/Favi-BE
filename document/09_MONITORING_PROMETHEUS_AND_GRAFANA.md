# 09 — Prometheus & Grafana Monitoring and Observability (Favi-BE)

## 1. Observability Stack Overview

The **Favi-BE** observability pipeline is engineered to provide end-to-end operational visibility, real-time telemetry, and health monitoring across the entire modular monolith architecture and its L7 NGINX gateway.

The stack utilizes an **Infrastructure-as-Code (IaC)** provisioning approach where datasources, dashboard schemas, scrape jobs, and collection intervals are declared in version-controlled configuration files and automatically provisioned on container startup.

```
                           [ Ingress Client Traffic ]
                                       │
                                       ▼ (Port 5000)
                     ┌───────────────────────────────────┐
                     │         NGINX Gateway             │
                     │  - Endpoints: /api, /chatHub...   │
                     │  - Metrics: /nginx_status         │
                     └─────────────────┬─────────────────┘
                                       │ (HTTP scrape)
                                       ▼
                     ┌───────────────────────────────────┐
                     │       favi-nginx-exporter         │
                     │       Port 9113 (Internal)        │
                     └─────────────────┬─────────────────┘
                                       │
                                       ▼ (Scrape interval: 5s)
                     ┌───────────────────────────────────┐
                     │         favi-prometheus           │
                     │         Port 9090:9090            │
                     │    - Time-series TSDB engine      │
                     │    - PromQL query evaluation      │
                     └─────────────────┬─────────────────┘
                                       │
                                       ▼ (Datasource: http://prometheus:9090)
                     ┌───────────────────────────────────┐
                     │          favi-grafana             │
                     │         Port 3050:3000            │
                     │  - Dashboard: favi-overview       │
                     │  - Real-time RPS & Connections    │
                     └───────────────────────────────────┘

         [ Auxiliary Health Monitoring ]
         favi-api-1 (Port 8080) ──▶ /health, /health/live, /health/ready, /health/details
         favi-api-2 (Port 8080) ──▶ /health, /health/live, /health/ready, /health/details
```

---

## 2. Docker Services & Network Topology

All monitoring components run within the isolated bridge network `favi-network` defined in [`docker-compose.yml`](../docker-compose.yml):

| Service Name | Container Name | Image & Version | Host : Container Port | Description |
| :--- | :--- | :--- | :---: | :--- |
| **`nginx-exporter`** | `favi-nginx-exporter` | `nginx/nginx-prometheus-exporter:1.1.0` | `9113` (Internal) | Scrapes `/nginx_status` and translates raw status counters into Prometheus gauge/counter metrics. |
| **`prometheus`** | `favi-prometheus` | `prom/prometheus:v2.51.0` | `9090:9090` | Core time-series database (TSDB) collecting and indexing telemetry every 5s. |
| **`grafana`** | `favi-grafana` | `grafana/grafana:10.4.1` | `3050:3000` | Telemetry visualization UI preloaded with custom dashboards and datasources. |

> [!NOTE]
> **Port 3050 Allocation**: Grafana listens internally on port `3000` but is intentionally mapped to host port **`3050`** (`3050:3000`). This ensures host ports `3000` and `3001` remain completely free for frontend Next.js applications and administrative web portals.

### Docker Compose Service Declaration

```yaml
# ===== Monitoring: NGINX Prometheus Exporter =====
nginx-exporter:
  image: nginx/nginx-prometheus-exporter:1.1.0
  container_name: favi-nginx-exporter
  restart: unless-stopped
  command:
    - -nginx.scrape-uri=http://gateway:80/nginx_status
  depends_on:
    - gateway
  networks:
    - favi-network

# ===== Monitoring: Prometheus =====
prometheus:
  image: prom/prometheus:v2.51.0
  container_name: favi-prometheus
  restart: unless-stopped
  volumes:
    - ./monitoring/prometheus/prometheus.yml:/etc/prometheus/prometheus.yml:ro
  ports:
    - "9090:9090"
  networks:
    - favi-network

# ===== Monitoring: Grafana =====
grafana:
  image: grafana/grafana:10.4.1
  container_name: favi-grafana
  restart: unless-stopped
  environment:
    - GF_SECURITY_ADMIN_PASSWORD=admin
    - GF_USERS_ALLOW_SIGN_UP=false
  volumes:
    - ./monitoring/grafana/provisioning:/etc/grafana/provisioning:ro
    - grafana_data:/var/lib/grafana
  ports:
    - "3050:3000"
  depends_on:
    - prometheus
  networks:
    - favi-network
```

---

## 3. Prometheus Architecture & Scrape Configurations

### 3.1 Global & Job Specifications (`prometheus.yml`)

The Prometheus collector configuration is located at [`monitoring/prometheus/prometheus.yml`](../monitoring/prometheus/prometheus.yml):

```yaml
global:
  scrape_interval: 5s
  evaluation_interval: 5s

scrape_configs:
  - job_name: 'prometheus'
    static_configs:
      - targets: ['localhost:9090']

  - job_name: 'nginx'
    static_configs:
      - targets: ['nginx-exporter:9113']
```

- **`scrape_interval: 5s`**: Configured with a granular 5-second sampling rate to provide high-resolution data during k6 load, stress, and spike test executions.
- **`evaluation_interval: 5s`**: Evaluates alerting rules and recording expressions every 5 seconds.
- **Job `prometheus`**: Monitors Prometheus self-health, internal heap memory, compaction timings, and query latency.
- **Job `nginx`**: Pulls Gateway metrics from `favi-nginx-exporter` via Docker service DNS (`nginx-exporter:9113`).

### 3.2 Gateway Metrics Exposure (`/nginx_status`)

NGINX exposes raw connection and request statistics via `stub_status` inside [`gateway/conf.d/gateway.conf`](../gateway/conf.d/gateway.conf):

```nginx
location /nginx_status {
    stub_status;
    access_log off;
    allow 127.0.0.1;
    allow 10.0.0.0/8;
    allow 172.16.0.0/12;
    allow 192.168.0.0/16;
    deny all;
}
```

- **Security Isolation**: Public client requests from outside the private Docker CIDRs (`172.16.0.0/12`, `10.0.0.0/8`, `192.168.0.0/16`) or localhost are rejected with `HTTP 403 Forbidden`.
- **Zero Log Overhead**: `access_log off;` prevents internal scraper polling from polluting Gateway access logs.

---

## 4. Telemetry Metrics Catalog

The NGINX exporter translates `stub_status` into standard Prometheus time-series metrics:

| Metric Name | Type | Description | Operational Significance |
| :--- | :---: | :--- | :--- |
| **`nginx_http_requests_total`** | Counter | Monotonically increasing count of all client requests accepted by NGINX. | Used with `rate()` to compute real-time throughput (Requests Per Second - RPS). |
| **`nginx_connections_active`** | Gauge | Current number of all active client connections (including connections in `reading`, `writing`, and `waiting` states). | Measures current concurrent user load and connection saturation. |
| **`nginx_connections_reading`** | Gauge | Number of connections where NGINX is actively reading request headers/body from clients. | Spikes indicate slow client uploads or network congestion. |
| **`nginx_connections_writing`** | Gauge | Number of connections where NGINX is processing or streaming response buffers back to clients. | High numbers indicate backend processing latency or slow consumer drain. |
| **`nginx_connections_waiting`** | Gauge | Idle keep-alive connections waiting for subsequent requests from clients. | Validates HTTP Keep-Alive connection reuse efficiency. |
| **`nginx_connections_accepted`** | Counter | Total client connections accepted by NGINX worker processes. | Tracks total volume of incoming TCP handshakes. |
| **`nginx_connections_handled`** | Counter | Total client connections successfully handled without being dropped. | If `accepted > handled`, indicates worker connection exhaustion (`worker_connections`). |
| **`up{job="nginx"}`** | Gauge | Health flag (`1` = healthy, `0` = unreachable). | Primary heartbeat for Gateway uptime monitoring and alerts. |

---

## 5. Grafana Provisioning & Dashboard Architecture

Grafana is provisioned **as code**, eliminating manual dashboard imports or GUI data source setups upon new deployments.

### 5.1 Datasource Provisioning (`datasource.yml`)

Located at [`monitoring/grafana/provisioning/datasources/datasource.yml`](../monitoring/grafana/provisioning/datasources/datasource.yml):

```yaml
apiVersion: 1

datasources:
  - name: Prometheus
    type: prometheus
    access: proxy
    url: http://prometheus:9090
    isDefault: true
    editable: false
```

- **Auto-Discovery**: Grafana connects to `http://prometheus:9090` over `favi-network`.
- **Default & Immutable**: Set as default datasource; `editable: false` prevents accidental UI modification in production.

### 5.2 Dashboard Provider Configuration (`dashboards.yml`)

Located at [`monitoring/grafana/provisioning/dashboards/dashboards.yml`](../monitoring/grafana/provisioning/dashboards/dashboards.yml):

```yaml
apiVersion: 1

providers:
  - name: 'Default'
    orgId: 1
    folder: 'Favi System'
    type: file
    disableDeletion: false
    editable: true
    options:
      path: /etc/grafana/provisioning/dashboards/json
```

Automatically scans and mounts all JSON dashboards located in `/etc/grafana/provisioning/dashboards/json` into Grafana's **"Favi System"** folder.

### 5.3 Provisioned Dashboard: `Favi Cluster Performance & Gateway Overview`

Defined in [`monitoring/grafana/provisioning/dashboards/json/favi-overview.json`](../monitoring/grafana/provisioning/dashboards/json/favi-overview.json) (`uid: "favi-overview"`):

#### Panel 1: Request Throughput (RPS)
- **Visualization**: Smooth Time-Series Line Chart (`timeseries`)
- **Unit**: `reqps` (Requests per second)
- **PromQL Target**:
  ```promql
  rate(nginx_http_requests_total[10s])
  ```
- **Purpose**: Displays the real-time velocity of incoming traffic through the Gateway. Instantaneously reflects load ramp-up during k6 benchmarks.

#### Panel 2: Active Client Connections
- **Visualization**: Multi-Series Time-Series Chart (`timeseries`)
- **Unit**: Integer Count (`short`)
- **PromQL Targets**:
  ```promql
  # Series 1: Total active concurrent connections
  nginx_connections_active

  # Series 2: Keep-alive idle connections
  nginx_connections_waiting
  ```
- **Purpose**: Evaluates connection pooling, Keep-Alive utilization, and connection drain behavior under stress.

```
+-----------------------------------------------------------------------------------------+
| Favi Cluster Performance & Gateway Overview                          Refresh: [ 5s v ]  |
+-----------------------------------------------------------------------------------------+
| [ API Gateway Metrics (NGINX) ]                                                         |
|                                                                                         |
|  +---------------------------------------+  +----------------------------------------+  |
|  | Request Throughput (RPS)              |  | Active Client Connections              |  |
|  |                                       |  |                                        |  |
|  | 1.2k |             /\                 |  | 250 |               /---\                |  |
|  | 800  |       /----/  \                |  | 150 |       /------/     \----           |  |
|  | 400  |  /---/         \---            |  | 50  |  /---/                  \---       |  |
|  | 0    +----------------------          |  | 0   +-----------------------------     |  |
|  |      Total RPS: 942 req/s             |  |     Active: 184 | Waiting: 64            |  |
|  +---------------------------------------+  +----------------------------------------+  |
+-----------------------------------------------------------------------------------------+
```

---

## 6. Core PromQL Queries for Operations & Debugging

Engineers and SREs can execute these PromQL queries directly in Prometheus (`http://localhost:9090`) or Grafana Explore:

### 6.1 Real-Time RPS per 10-second Window
```promql
rate(nginx_http_requests_total[10s])
```

### 6.2 Total Requests Processed Over the Last 1 Hour
```promql
increase(nginx_http_requests_total[1h])
```

### 6.3 Worker Connection Saturation Ratio (%)
Evaluates how close NGINX is to exhausting its configured `worker_connections` limit:
```promql
(nginx_connections_active / 4096) * 100
```

### 6.4 Active Client Workload vs. Idle Keep-Alive Ratio
Determines whether connected clients are actively transmitting data or holding idle keep-alive sockets:
```promql
(nginx_connections_active - nginx_connections_waiting) / nginx_connections_active
```

### 6.5 Dropped TCP Handshake Detection
Identifies if NGINX failed to handle an accepted connection due to file descriptor or resource limits:
```promql
nginx_connections_accepted - nginx_connections_handled
```

---

## 7. Recommended Prometheus Alerting Rules

In production environments, configure Prometheus alerting rules (`alerts.yml`) to proactively notify engineering channels (Slack, PagerDuty, Discord):

```yaml
groups:
  - name: favi_gateway_alerts
    rules:
      # 1. Gateway Down Alert
      - alert: GatewayDown
        expr: up{job="nginx"} == 0
        for: 30s
        labels:
          severity: critical
        annotations:
          summary: "NGINX Gateway is unreachable"
          description: "Prometheus cannot scrape favi-nginx-exporter for > 30s. Traffic ingress is halted."

      # 2. Connection Saturation Warning
      - alert: HighActiveConnections
        expr: nginx_connections_active > 2000
        for: 1m
        labels:
          severity: warning
        annotations:
          summary: "High concurrent connection count on NGINX Gateway"
          description: "Active connections reached {{ $value }}, approaching replica pool limits."

      # 3. Connection Drop Detection
      - alert: GatewayConnectionDropped
        expr: rate(nginx_connections_accepted[1m]) - rate(nginx_connections_handled[1m]) > 0
        for: 15s
        labels:
          severity: critical
        annotations:
          summary: "NGINX is dropping incoming TCP client connections"
          description: "Accepted connections exceed handled connections. Verify worker_connections and system ulimits."

      # 4. Unusual Traffic Surge (Potential DDoS / Spike)
      - alert: TrafficSurgeSpike
        expr: rate(nginx_http_requests_total[1m]) > 3000
        for: 30s
        labels:
          severity: warning
        annotations:
          summary: "Rapid throughput surge detected"
          description: "Throughput exceeded 3,000 req/s. Verify rate limiting zones and backend replica scaling."
```

---

## 8. Application Health Probes & Deep Monitoring

In addition to NGINX metric scraping, the backend exposes multi-tiered ASP.NET Core health endpoints configured in [`Program.cs`](../Favi-BE/Favi-BE.API/Program.cs):

### 8.1 Health Probes Catalog

```
                    ┌────────────────────────────────────────┐
                    │       ASP.NET Core Application         │
                    └───────────────────┬────────────────────┘
                                        │
           ┌────────────────────────────┼────────────────────────────┐
           ▼                            ▼                            ▼
      GET /health               GET /health/ready            GET /health/details
   (Liveness / Ping)          (Dependency Readiness)         (JSON Diagnostic UI)
   - Zero DB/Cache query      - PostgreSQL Ping              - Per-module status
   - Fast return (200 OK)     - Redis Connection             - Latency in milliseconds
   - Used by LB failover      - Vector API Probe             - For SRE troubleshooting
```

1. **`GET /health` & `GET /health/live`**:
   - **Type**: Lightweight Liveness Probe.
   - **Implementation**: Bypasses all database checks and external dependencies. Returns `{"status": "ok"}` or `{"status": "alive"}` immediately.
   - **Target**: Docker Compose health checks and NGINX passive failover (`proxy_next_upstream`).

2. **`GET /health/ready`**:
   - **Type**: Readiness Probe.
   - **Implementation**: Executes checks tagged with `"ready"`. Verifies network connectivity and active connection pools to:
     - PostgreSQL database cluster (`favi-postgres:5432`).
     - Redis distributed cache & lock server (`favi-redis:6379`).
     - Vector Index FastAPI monolith (`favi-vector-api:8080`).
   - **Target**: Decides whether the replica is eligible to receive user ingress traffic.

3. **`GET /health/details`**:
   - **Type**: Diagnostic Monitoring Probe.
   - **Implementation**: Formatted using `UIResponseWriter.WriteHealthCheckUIResponse` (`HealthChecks.UI.Client`).
   - **Payload**: Detailed JSON report including individual dependency names, execution durations, and error descriptions.

---

## 9. Observability Verification with k6 Benchmarks

During performance testing suites (documented in [`07_TESTING_AND_BENCHMARKING.md`](./07_TESTING_AND_BENCHMARKING.md)), Prometheus and Grafana serve as the **authoritative verification telemetry**:

### 9.1 Verification Workflow

1. **Open Grafana Dashboard**:
   Navigate to `http://localhost:3050` (or `http://localhost:3050/d/favi-overview`).
2. **Execute k6 Test Scenario**:
   ```powershell
   # Run steady-state feed scrolling load test
   k6 run Favi-BE/Favi-BE.API/k6-tests/load/scenario-2-feed-refresh-steady-users.js
   ```
3. **Observe Telemetry Correlation**:
   - **Throughput Curve**: Verify that `Request Throughput (RPS)` climbs in synchronization with k6 Virtual User (VU) ramp stages.
   - **Connection Pool Stability**: Verify `Active Client Connections` plateaus at expected levels without runaway exponential growth (which would indicate unclosed sockets or connection leaks).
   - **Soak Test Memory Leak Audit**: During a 2-hour soak test (`scenario-a5-soak-sustained-endurance-2h.js`), confirm that connection counts return to idle baseline (~0–5 conns) immediately after test termination.

---

## 10. Operational Runbook

### 10.1 Starting & Restarting the Monitoring Stack

To start only the monitoring and gateway services:
```powershell
docker compose up -d gateway nginx-exporter prometheus grafana
```

To view real-time log outputs:
```powershell
# Follow Prometheus logs
docker compose logs -f prometheus

# Follow Grafana logs
docker compose logs -f grafana

# Follow NGINX exporter logs
docker compose logs -f nginx-exporter
```

### 10.2 Service Verification Checklist

Run these commands to verify operational health:

```powershell
# 1. Verify NGINX status endpoint directly
curl -i http://localhost:5000/nginx_status

# 2. Verify NGINX Prometheus Exporter metrics endpoint
curl -i http://localhost:9113/metrics

# 3. Verify Prometheus target discovery status
curl -s http://localhost:9090/api/v1/targets | ConvertFrom-Json | Select-Object -ExpandProperty data | Select-Object -ExpandProperty activeTargets | Format-Table job, health, scrapeUrl

# 4. Verify Grafana HTTP liveness
curl -i http://localhost:3050/api/health
```

### 10.3 Accessing Grafana Dashboard

- **URL**: `http://localhost:3050`
- **Default Credentials**:
  - **Username**: `admin`
  - **Password**: `admin` (Configured via `GF_SECURITY_ADMIN_PASSWORD=admin`)
- **Direct Link to Provisioned Dashboard**:
  `http://localhost:3050/d/favi-overview/favi-cluster-performance-gateway-overview`

### 10.4 Modifying and Exporting New Dashboards

To add or modify dashboards while preserving them in version control:
1. Make your dashboard adjustments directly within the Grafana web UI (`http://localhost:3050`).
2. Click **Dashboard Settings (Gear Icon)** -> **JSON Model**.
3. Copy the entire JSON schema.
4. Replace or add the new file in [`monitoring/grafana/provisioning/dashboards/json/`](../monitoring/grafana/provisioning/dashboards/json/).
5. Commit the JSON file to Git. Any future `docker compose up` on any developer or staging machine will automatically provision the updated dashboard!

---

## 11. Roadmap: Future Observability Enhancements

As Favi scales towards production multi-node clusters, the following enhancements are architected:

1. **.NET OpenTelemetry / Prometheus In-Process Exporter**:
   - Add `OpenTelemetry.Exporter.Prometheus.AspNetCore` to `Favi-BE.API` exposing `/metrics`.
   - Collect .NET runtime statistics: GC pause time (`dotnet_gc_pause_seconds`), ThreadPool queue length (`dotnet_threadpool_queue_length`), and EF Core query execution durations.
2. **PostgreSQL Exporter (`wrouesnel/postgres_exporter`)**:
   - Monitor database connection pool saturation, cache hit ratio (`buffer cache vs disk read`), and deadlocks.
3. **Redis Exporter (`oliver006/redis_exporter`)**:
   - Monitor Redis memory fragmentation, eviction rates, and cache hit/miss percentages.
4. **Distributed Tracing with OpenTelemetry & Jaeger / Tempo**:
   - Propagate `X-Request-ID` from NGINX Gateway down to MediatR pipeline behaviors and background Outbox processors for distributed waterfall trace inspection.

