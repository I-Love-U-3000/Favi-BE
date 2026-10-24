# Favi-BE Technical Documentation Index

Welcome to the comprehensive technical documentation for the **Favi Backend (Favi-BE)** system — a modern image-sharing social media platform built on a **Modular Monolith, DDD, CQRS, Outbox/Inbox patterns, and NGINX L7 Gateway** architecture.

---

## Technical Documentation Directory

| # | Document | Detailed Description |
|---|---|---|
| **00** | [00_OVERVIEW_AND_ARCHITECTURE.md](./00_OVERVIEW_AND_ARCHITECTURE.md) | System overview, Docker topology, core architectural principles, DDD, Modular Monolith & Clean Architecture. |
| **01** | [01_MODULES_AND_BOUNDED_CONTEXTS.md](./01_MODULES_AND_BOUNDED_CONTEXTS.md) | 9 Bounded Contexts / Modules and BuildingBlocks shared kernel, Aggregate Roots, Event catalog, dependency matrix. |
| **02** | [02_LAYERS_AND_DESIGN_PATTERNS.md](./02_LAYERS_AND_DESIGN_PATTERNS.md) | Layered structure (Domain - Application - Infrastructure - API Host), MediatR pipeline behaviors, Strangler Fig pattern. |
| **03** | [03_IDEMPOTENCY_AND_RELIABILITY.md](./03_IDEMPOTENCY_AND_RELIABILITY.md) | Two-layer idempotency architecture: HTTP `Idempotency-Key` via Redis distributed lock and Async Outbox/Inbox consumer deduplication. |
| **04** | [04_NETWORKING_GATEWAY_AND_LOAD_BALANCING.md](./04_NETWORKING_GATEWAY_AND_LOAD_BALANCING.md) | NGINX API Gateway, load balancing (`least_conn`, sticky session `ip_hash` for SignalR), rate limiting, and failover mechanisms. |
| **05** | [05_SCALING_PERFORMANCE_AND_OBSERVABILITY.md](./05_SCALING_PERFORMANCE_AND_OBSERVABILITY.md) | Horizontal scaling strategies, PostgreSQL query optimization (indexing, keyset pagination, split queries, batching, EXPLAIN), Redis caching, Prometheus & Grafana. |
| **06** | [06_VERSION_CONTROL_AND_DATABASE_MIGRATIONS.md](./06_VERSION_CONTROL_AND_DATABASE_MIGRATIONS.md) | Git version control workflows, zero-downtime EF Core migrations, Optimistic Locking & table record versioning, deterministic seed pipeline. |
| **07** | [07_TESTING_AND_BENCHMARKING.md](./07_TESTING_AND_BENCHMARKING.md) | Multi-tiered testing strategy: Architecture tests, smoke tests, functional tests, k6 load/stress/spike/soak tests & Grafana verification. |

---

## Mandatory Principles for Engineers & AI Agents

1. **Review Before Execution**: Before adding features, fixing bugs, or refactoring any module, thoroughly read the relevant documentation in this directory.
2. **Synchronous Documentation Updates**: Whenever architectural logic, API contracts, schemas, or processing flows change, update the corresponding documentation within the same commit/task.
3. **Respect Module Boundaries**: Never directly access DB contexts or internal services of another module; all cross-module interactions must go through Integration Events (Outbox/Inbox) or public Facade/Contract interfaces.
