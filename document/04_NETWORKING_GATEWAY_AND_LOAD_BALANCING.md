# 04 — NGINX API Gateway, Load Balancing & Networking (Favi-BE)

## 1. Role of the NGINX Gateway (L7 Reverse Proxy & Load Balancer)

In the Favi architecture, NGINX acts as the **Single Point of Ingress**, listening on host port `5000` (mapped from container port `80`).
All external clients (Next.js Web, Mobile Apps, Postman) communicate exclusively through this Gateway.

The configuration is organized into two files:
- `gateway/nginx.conf`: Configures workers, buffers, gzip compression, rate-limiting shared memory zones, and WebSocket connection upgrade maps.
- `gateway/conf.d/gateway.conf`: Defines upstream server clusters, location routing, security headers, and passive failover rules.

---

## 2. Upstream Clusters & Load Balancing Algorithms

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

### 2.1 REST API Cluster (`favi_api_cluster`)
- **Algorithm**: `least_conn` (routes incoming requests to the replica with the lowest active connection count).
- **Members**: `favi-api-1:8080` and `favi-api-2:8080`.
- **Passive Failover Mechanism**:
  ```nginx
  proxy_next_upstream error timeout invalid_header http_502 http_503 http_504;
  proxy_next_upstream_tries 2;
  proxy_next_upstream_timeout 10s;
  ```
  If a replica crashes, restarts, or returns 502/503/504 errors, NGINX automatically retries on the remaining healthy replica within 10 seconds without failing the user's request.
- **Keepalive Connections**: Pools 64 idle connections to minimize TCP handshake overhead.

### 2.2 SignalR Realtime Cluster (`favi_signalr_cluster`)
- **Challenge**: The SignalR protocol requires an initial HTTP negotiation step (`/negotiate`) prior to upgrading to a persistent WebSocket. If negotiation lands on Node 1 but the subsequent WebSocket upgrade targets Node 2, the connection is rejected (`404 Connection ID not found`).
- **Solution**: The **`ip_hash` (Sticky Sessions)** algorithm. Hashes the client's IP address to ensure all requests from that client consistently hit the same backend replica.
- **WebSocket Configuration**:
  - `proxy_http_version 1.1;`
  - `proxy_set_header Upgrade $http_upgrade;`
  - `proxy_set_header Connection $connection_upgrade;`
  - `proxy_read_timeout 3600s;` (keeps long-lived connection active for up to 1 hour)
  - `proxy_buffering off;` (disables response buffering for zero-latency frame delivery)

### 2.3 Vector & AI API Cluster (`vector_api_cluster`)
- Routes AI search and NSFW content moderation requests:
  `^/api/v1/(search/vector|nsfw)` directly to the FastAPI container `vector-index-api:8080`.

---

## 3. Rate Limiting Tiers

NGINX utilizes shared memory zones (10MB per zone, tracking ~160,000 distinct IP addresses) to mitigate DoS, brute-force attacks, and API abuse:

| Zone Name | Allowed Rate | Burst Queue | Applicable Endpoints | Target Protection |
|---|---|---|---|---|
| `auth_limit` | **10 requests / min** | `burst=5 nodelay` | `/api/auth/(login\|register\|forgot-password\|reset-password)` | Prevents credential stuffing & automated spam registration. |
| `search_limit` | **30 requests / min** | `burst=10 nodelay` | `/api/v1/(search/vector\|nsfw)` | Protects compute-heavy CLIP and vector model resources. |
| `api_limit` | **150 requests / min** | `burst=30 nodelay` | All standard business APIs (`/api/*`) | Prevents data scraping and API request flooding. |

- When limits are exceeded, NGINX immediately terminates the request with HTTP **`429 Too Many Requests`**.

---

## 4. Static Asset Offloading & File Upload Limits

### 4.1 Static Seed Assets Offload (`/seed-assets/`)
- Rather than burdening ASP.NET Core worker threads with disk I/O and streaming static seed images, NGINX directly serves them from the mounted volume `/var/www/seed-assets/`.
- Configured with long-term browser caching:
  ```nginx
  location /seed-assets/ {
      alias /var/www/seed-assets/;
      expires 7d;
      add_header Cache-Control "public, max-age=604800, immutable";
      access_log off;
  }
  ```

### 4.2 File Upload Limits
- Global default body size limit is set to `client_max_body_size 2M;`.
- Post creation, avatar uploads, and media attachments:
  `location ~* ^/api/(posts|profile/avatar|media|upload)` are granted an extended limit of **`25M`** (`client_max_body_size 25M;`).

---

## 5. Distributed Tracing & Security Headers

- **`X-Request-ID` Header**: NGINX generates a unique UUID `$request_id` for each request (or propagates one from the client) and forwards it via `X-Request-ID`. Backend Serilog attaches this ID to all structured logs, and the Gateway echoes it back in client response headers for end-to-end trace correlation.
- **Security Headers**:
  - `X-Content-Type-Options: nosniff` (mitigates MIME-type sniffing).
  - `X-Frame-Options: DENY` (mitigates Clickjacking).
- **Gzip Compression**: Automatically compresses text, JSON, JavaScript, and XML payloads larger than 1024 bytes (compression level 6).
