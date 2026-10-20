# 00 — Tổng Quan Hệ Thống & Kiến Trúc Tổng Thể (Favi-BE)

## 1. Giới thiệu Dự Án
**Favi** là nền tảng mạng xã hội chia sẻ hình ảnh hiện đại, cung cấp trải nghiệm tương tác trực quan thời gian thực, bảng tin cá nhân hóa (personalized feed), tìm kiếm thông minh bằng trí tuệ nhân tạo (Multimodal Semantic Search qua CLIP embeddings), câu chuyện 24h (Stories), trò chuyện trực tiếp (Realtime Messaging & WebRTC call), và hệ thống quản trị/kiểm duyệt toàn diện (Moderation & Admin Portal).

Hệ thống backend (`Favi-BE`) được thiết kế theo mô hình **Modular Monolith** kết hợp nguyên lý **Domain-Driven Design (DDD)**, **CQRS (Command Query Responsibility Segregation)**, **Outbox/Inbox Patterns**, và chạy sau cụm **NGINX API Gateway / L7 Load Balancer**.

---

## 2. Mô hình Kiến trúc Cấp cao (High-Level Architecture)

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

## 3. Topo Hạ Tầng & Dịch Vụ Docker

Tất cả các dịch vụ được liên kết trong mạng nội bộ ảo `favi-network` (bridge mode):

| Container Name | Service / Công nghệ | Port Host:Container | Mục đích & Trách nhiệm |
|---|---|---|---|
| `favi-gateway` | NGINX Alpine (L7 Reverse Proxy) | `5000:80` | Entrypoint duy nhất cho Client; phân luồng API, rate limit, WebSocket sticky session. |
| `favi-api-1` | ASP.NET Core 9 (.NET API Replica 1) | `8080` (Internal) | Xử lý business logic, REST API, SignalR Hubs, Outbox/Inbox worker. |
| `favi-api-2` | ASP.NET Core 9 (.NET API Replica 2) | `8080` (Internal) | Replica mở rộng ngang, đảm bảo tính sẵn sàng (High Availability) & failover. |
| `favi-postgres` | PostgreSQL 15 | `5432:5432` | Relational Storage chính cho tất cả Bounded Contexts; lưu Outbox/Inbox events. |
| `favi-redis` | Redis 7 Alpine | `6379:6379` | Distributed Caching, Distributed Locks, In-Flight Idempotency guard, Rate limiting counters. |
| `favi-vector-api` | Python FastAPI + OpenCLIP | `18080:8080` | Đọc hình ảnh và sinh 512-dim vector embeddings, phát hiện nội dung NSFW. |
| `favi-qdrant` | Qdrant Vector Engine | `6333:6333` | Lưu trữ vector collection `posts_demo`, phục vụ tìm kiếm tương đồng vector k-NN. |
| `favi-nginx-exporter` | Prometheus NGINX Exporter | Internal | Thu thập metrics từ NGINX `/nginx_status` chuyển tới Prometheus. |
| `favi-prometheus` | Prometheus v2.51.0 | `9090:9090` | Thu thập và lưu trữ time-series metrics từ Gateway và .NET API. |
| `favi-grafana` | Grafana 10.4.1 | `3050:3000` | Giao diện trực quan hóa dashboard giám sát hiệu năng hệ thống. |

---

## 4. Các Nguyên Lý Thiết Kế Cốt Lõi (Core Principles)

### 4.1 Modular Monolith — Ranh giới rõ ràng
Thay vì phân rã thành các Microservices quá sớm gây tốn chi phí vận hành network và phân tán transaction, Favi sử dụng **Modular Monolith**:
- Tất cả module chạy trong cùng runtime nhưng được phân tách thành các **Project (.csproj) độc lập**.
- **Không chia sẻ Entity**: Mỗi module có Entity và DbContext Model riêng.
- **Không gọi chéo trực tiếp**: Tuyệt đối cấm Module A inject service hoặc reference namespace nội bộ của Module B.
- **Giao tiếp liên module**: Được thực hiện qua **Integration Events** ghi vào Outbox hoặc thông qua **Facade/Query Contracts** được đăng ký ở tầng Application.

### 4.2 Clean Architecture / Hexagonal (Ports & Adapters)
Mỗi module đều tuân thủ cấu trúc phân tầng nghiêm ngặt:
- **Domain** nằm ở trung tâm nhất, không phụ thuộc vào bất kỳ framework hoặc library bên ngoài nào.
- **Application** chứa use case handlers (Commands & Queries) và định nghĩa các Ports (Interfaces).
- **Infrastructure** implement các Ports đó (Database adapters, external APIs, Outbox/Inbox processors).
- **API** đóng vai trò Composition Root và HTTP dispatcher mỏng.

### 4.3 CQRS (Command Query Responsibility Segregation)
- **Command Path (Ghi/Thay đổi trạng thái)**: Đi qua `IMediator.Send(Command)` → `CommandHandler` → EF Core Tracking DbContext → Xác thực Business Rules → Tạo Domain Events → Lưu Outbox trong cùng 1 transaction.
- **Query Path (Đọc dữ liệu)**: Đi qua `IMediator.Send(Query)` → `QueryHandler` → EF Core với `.AsNoTracking()` (và có lộ trình chuyển sang Dapper cho các hot path) → trả về DTOs tối ưu cho client.

### 4.4 Đảm bảo Tính Nhất Quán & Tin Cậy (Transactional Outbox + Inbox)
Không thực hiện bất kỳ side effect qua network (như gửi SignalR notification, call webhook, upload ngoài) trực tiếp bên trong database transaction.
Thay vào đó, side effect được serialize thành **Outbox Event** và commit nguyên tử cùng bảng dữ liệu. Background worker (`OutboxProcessor`) sẽ quét và chuyển tới consumer được bảo vệ bởi **Inbox Pattern** (Idempotency unit `(messageId, consumerName)`).
