# 00 — System Overview & High-Level Architecture (Favi-BE)

## 1. Project Introduction
**Favi** is a modern image-sharing social media platform delivering interactive realtime experiences, personalized feeds, AI-powered intelligent search (Multimodal Semantic Search via CLIP embeddings), 24-hour Stories, direct realtime communication (Realtime Messaging & WebRTC calling), and a comprehensive administration/moderation portal.

The backend system (`Favi-BE`) is architected as a **Modular Monolith** combining principles from **Domain-Driven Design (DDD)**, **CQRS (Command Query Responsibility Segregation)**, **Outbox/Inbox Patterns**, running behind an **NGINX API Gateway / L7 Load Balancer**.

---

## 2. High-Level Architecture

```
                            [ Web & Mobile Clients ]
                                       │
                                       ▼ (HTTP / HTTPS / WSS: Port 5000)
                     ┌───────────────────────────────────┐
                     │    NGINX API Gateway & L7 LB      │
                     │  - Rate Limiting (auth/search/api)│
                     │  - Sticky Session for SignalR     │
                     │  - Passive Failover (502/503/504) │
                     │  - Static Asset Offload (/seed)   │
                     └──────┬──────────────────────┬─────┘
                            │                      │
             (least_conn / ip_hash)                │ (least_conn)
             ┌──────────────┴─────────────┐        ▼
             ▼                            ▼   ┌─────────────────────────────┐
    ┌─────────────────┐          ┌─────────────────┐ │  Vector Index API (FastAPI)  │
    │   favi-api-1    │          │   favi-api-2    │ │  - OpenCLIP ViT-B-32 Embed   │
    │  ASP.NET Core   │          │  ASP.NET Core   │ │  - NSFW Detection            │
    │   (Replica 1)   │          │   (Replica 2)   │ └──────────────┬──────────────┘
    └────────┬────────┘          └────────┬────────┘                │
             │                            │                         │
             └──────────────┬─────────────┘                         │
                            │                                       ▼
       ┌────────────────────┼────────────────────┐        ┌───────────────────┐
       ▼                    ▼                    ▼        │   Qdrant Vector   │
┌──────────────┐     ┌──────────────┐     ┌──────────────┐│     Database      │
│  PostgreSQL  │     │ Redis Cache  │     │ Cloudinary & ││ (Vector Search)   │
│   Database   │     │ & Locks/Idem │     │   Supabase   │└───────────────────┘
│  (Port 5432) │     │ (Port 6379)  │     │ Storage/Auth │
└──────────────┘     └──────────────┘     └──────────────┘
```

---

## 3. Infrastructure Topology & Docker Services

All services communicate over an internal virtual bridge network named `favi-network`:

| Container Name | Service / Technology | Host:Container Port | Purpose & Responsibilities |
|---|---|---|---|
| `favi-gateway` | NGINX Alpine (L7 Reverse Proxy) | `5000:80` | Single entrypoint for clients; API routing, rate limiting, and SignalR WebSocket sticky sessions. |
| `favi-api-1` | ASP.NET Core 9 (.NET API Replica 1) | `8080` (Internal) | Core business logic, REST APIs, SignalR Hubs, Outbox/Inbox background workers. |
| `favi-api-2` | ASP.NET Core 9 (.NET API Replica 2) | `8080` (Internal) | Horizontally scaled replica ensuring High Availability (HA) and automatic failover. |
| `favi-postgres` | PostgreSQL 15 | `5432:5432` | Primary relational storage for all Bounded Contexts; stores Outbox/Inbox events. |
| `favi-redis` | Redis 7 Alpine | `6379:6379` | Distributed caching, distributed locks, in-flight idempotency guards, and rate limiting counters. |
| `favi-vector-api` | Python FastAPI + OpenCLIP | `18080:8080` | Image processing, generating 512-dim vector embeddings, and NSFW content detection. |
| `favi-qdrant` | Qdrant Vector Engine | `6333:6333` | Stores `posts_demo` vector collection, serving k-NN approximate nearest neighbor similarity searches. |
| `favi-nginx-exporter` | Prometheus NGINX Exporter | Internal | Scrapes internal metrics from NGINX `/nginx_status` and feeds Prometheus. |
| `favi-prometheus` | Prometheus v2.51.0 | `9090:9090` | Collects and retains time-series metrics from the Gateway and .NET API. |
| `favi-grafana` | Grafana 10.4.1 | `3050:3000` | Visualization dashboard for real-time system performance and health monitoring. |

---

## 4. Core Design Principles

### 4.1 Modular Monolith — Explicit Boundaries
Instead of premature microservice decomposition—which introduces network latency overhead and distributed transaction complexity—Favi employs a **Modular Monolith**:
- All modules run in a unified runtime process but are isolated into **independent projects (`.csproj`)**.
- **No Shared Entities**: Each module encapsulates its own Entities and DbContext models.
- **No Direct Cross-Module Invocations**: Module A is strictly prohibited from injecting services or referencing internal namespaces of Module B.
- **Inter-Module Communication**: Handled exclusively via **Integration Events** published through the Transactional Outbox or via registered **Facade/Query Contracts** at the Application layer.

### 4.2 Clean Architecture / Hexagonal (Ports & Adapters)
Every module strictly adheres to a clean layered architecture:
- **Domain**: Positioned at the very core, zero dependencies on external frameworks or third-party libraries.
- **Application**: Contains use case handlers (Commands & Queries) and defines Ports (Interfaces).
- **Infrastructure**: Implements these Ports (Database adapters, external APIs, Outbox/Inbox processors).
- **API**: Serves as the Composition Root and a thin HTTP dispatcher.

### 4.3 CQRS (Command Query Responsibility Segregation)
- **Command Path (State Mutations)**: Routed via `IMediator.Send(Command)` → `CommandHandler` → EF Core Tracking DbContext → Business Rule Validations → Domain Events → Atomically committed to Outbox in a single transaction.
- **Query Path (Data Reads)**: Routed via `IMediator.Send(Query)` → `QueryHandler` → EF Core with `.AsNoTracking()` (with a future roadmap for Dapper on hot paths) → Directly returns optimized DTOs to clients.

### 4.4 Consistency & Reliability Guarantees (Transactional Outbox + Inbox)
No network-bound side effects (such as emitting SignalR notifications, invoking webhooks, or external storage uploads) are executed directly inside a database transaction.
Instead, side effects are serialized into **Outbox Events** and committed atomically alongside domain entity tables. A background worker (`OutboxProcessor`) scans and dispatches them to consumers guarded by the **Inbox Pattern** (Idempotency key: `(messageId, consumerName)`).
