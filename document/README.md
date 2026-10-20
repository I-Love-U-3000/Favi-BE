# Favi-BE Technical Documentation Index

Chào mừng bạn đến với tài liệu kỹ thuật hoàn chỉnh của hệ thống **Favi Backend (Favi-BE)** — nền tảng mạng xã hội chia sẻ hình ảnh hiện đại xây dựng trên kiến trúc **Modular Monolith, DDD, CQRS, Outbox/Inbox và NGINX L7 Gateway**.

---

## Danh mục tài liệu kỹ thuật

| # | Tài liệu | Mô tả chi tiết |
|---|---|---|
| **00** | [00_OVERVIEW_AND_ARCHITECTURE.md](./00_OVERVIEW_AND_ARCHITECTURE.md) | Tổng quan hệ thống, topology Docker, nguyên lý thiết kế, DDD, Modular Monolith & Clean Architecture. |
| **01** | [01_MODULES_AND_BOUNDED_CONTEXTS.md](./01_MODULES_AND_BOUNDED_CONTEXTS.md) | 9 Bounded Contexts / Modules và BuildingBlocks shared kernel, Aggregate Roots, Event catalog, matrix quan hệ. |
| **02** | [02_LAYERS_AND_DESIGN_PATTERNS.md](./02_LAYERS_AND_DESIGN_PATTERNS.md) | Cấu trúc phân tầng (Domain - Application - Infrastructure - API Host), MediatR pipeline behaviors, Strangler Fig. |
| **03** | [03_IDEMPOTENCY_AND_RELIABILITY.md](./03_IDEMPOTENCY_AND_RELIABILITY.md) | Kiến trúc Idempotency 2 lớp: HTTP `Idempotency-Key` qua Redis distributed lock và Async Outbox/Inbox consumer dedup. |
| **04** | [04_NETWORKING_GATEWAY_AND_LOAD_BALANCING.md](./04_NETWORKING_GATEWAY_AND_LOAD_BALANCING.md) | NGINX API Gateway, Load Balancing (`least_conn`, sticky session `ip_hash` cho SignalR), Rate limiting, Failover. |
| **05** | [05_SCALING_PERFORMANCE_AND_OBSERVABILITY.md](./05_SCALING_PERFORMANCE_AND_OBSERVABILITY.md) | Chiến lược mở rộng ngang (horizontal scaling), Read/Write split, Redis caching, Prometheus & Grafana monitoring. |
| **06** | [06_VERSION_CONTROL_AND_DATABASE_MIGRATIONS.md](./06_VERSION_CONTROL_AND_DATABASE_MIGRATIONS.md) | Quy trình Version Control (Git), Strangler binary rollback, EF Core Migrations không downtime, Seed Pipeline. |
| **07** | [07_TESTING_AND_BENCHMARKING.md](./07_TESTING_AND_BENCHMARKING.md) | Chiến lược kiểm thử đa tầng: Architecture Tests, Smoke, Functional, k6 Load/Stress/Spike/Soak & đối soát Grafana. |

---

## Nguyên tắc bắt buộc đối với Kỹ sư & AI Agent

1. **Tra cứu trước khi thực thi**: Trước khi thêm tính năng, sửa bug hoặc refactor bất kỳ module nào, bắt buộc đọc kỹ tài liệu liên quan trong thư mục này.
2. **Cập nhật sau khi thay đổi**: Khi thay đổi logic kiến trúc, API contract, schema hay flow xử lý, phải cập nhật tài liệu tương ứng ngay trong cùng commit/task.
3. **Tuân thủ ranh giới module**: Tuyệt đối không gọi trực tiếp DB context hay internal services của module khác; mọi giao tiếp liên module bắt buộc qua Integration Events (Outbox/Inbox) hoặc Facade/Contract interfaces.
