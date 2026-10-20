# Copilot & Agent Operating Instructions

You must work with exceptional precision and system-level depth because GPT Codex and rigorous code review tools will inspect and validate your output.

---

## 1. Mandatory Documentation Protocol

### 1.1 Before Starting Any Task (Search & Review First)
- **Mandatory Document Search**: Before modifying any code, creating new files, or proposing architectural changes, the agent **MUST search for and read relevant technical documents** in the `document/` folder (`Favi-BE/document` and/or `Favi-FE/document`).
- Review the specific module boundaries, aggregate roots, layer responsibilities, idempotency requirements, NGINX routing rules, and database invariants established for the affected areas.
- Never write code based on assumptions if a document exists covering that domain.

### 1.2 After Making Any Changes (Keep Documents Synchronized)
- **Mandatory Documentation Update**: After implementing, refactoring, or modifying any feature, architecture, schema, API endpoint, or data flow, the agent **MUST immediately update the corresponding documents** in `document/` to reflect the changes.
- Implementation and documentation must stay 100% in sync at all times. Leaving documents stale or outdated is strictly forbidden.

---

## 2. Core Architectural & Implementation Principles

### 2.1 Modular Monolith & Strict Boundary Enforcement
- **Physical Isolation**: Keep modules decoupled. No direct cross-module assembly references between module projects.
- **No Shared Entities**: Each module owns its aggregates and database tables. Never query another module's DbContext or internal tables directly.
- **Cross-Module Communication**:
  - Asynchronous / Side-effects: Must use **Integration Events** dispatched via the **Outbox/Inbox pattern**.
  - Synchronous Queries / Reads: Must use **Application Facades** or dedicated **Query Reader Contracts** registered at the host level.
- **Dependency Injection**: **DO NOT** add raw service registrations directly to `Program.cs`. Instead, encapsulate them inside `IServiceCollection` extension methods grouped by responsibility (e.g., `AddInfrastructure`, `AddAuthModule`, `AddNotificationsModule`). Each module must have its own extension method.

### 2.2 CQRS & Clean Architecture
- **Onion Layers**: Domain (Entities, Value Objects, Rules) -> Application (Commands, Queries, DTOs, Validators, Ports) -> Infrastructure (EF Core configurations, Adapters, Outbox/Inbox) -> API Host.
- **Write Path (Commands)**: Use `IMediator.Send(ICommand)`. Handlers mutate aggregates, execute domain invariants via `CheckRule`, and commit via EF Core tracking within an atomic transaction managed by `TransactionBehavior`.
- **Read Path (Queries)**: Use `IMediator.Send(IQuery)`. Handlers must use `.AsNoTracking()` (or Dapper projections) for zero tracking overhead and optimal throughput.
- **Pipeline Behaviors**: Respect the MediatR pipeline order: `ValidationBehavior` (FluentValidation) -> `LoggingBehavior` (Serilog + RequestId) -> `PerformanceBehavior` -> `TransactionBehavior` -> Handler.

### 2.3 Dual-Layer Idempotency & Reliability
- **HTTP Gateway Level**: All mutating HTTP requests (`POST`, `PUT`, `PATCH`, `DELETE`) under `/api/` require a valid `Idempotency-Key` header (up to 128 characters).
  - Managed by `IdempotencyMiddleware` via Redis distributed locking (`favi:idempotency:{userId}:{key}`).
  - In-flight requests return `409 Conflict` (`IDEMPOTENT_OPERATION_IN_PROGRESS`).
  - Successful 2xx responses are cached for 5 minutes and replayed upon identical key resubmission.
  - Failures and exceptions immediately delete the Redis lock to allow clean retries.
- **Event Consumer Level**: Every outbox consumer must guard processing via `IInbox.TryStartProcessingAsync(messageId, consumerName)` and mark completion via `MarkProcessedAsync`. Never perform network side-effects inside database commit transactions.

### 2.4 Load Balancer, Gateway & Realtime Networking
- Respect the NGINX L7 Gateway routing and rate-limiting zones (`auth_limit` 10r/m, `search_limit` 30r/m, `api_limit` 150r/m).
- Realtime WebSockets (`/chatHub`, `/notificationHub`, `/callHub`) require `ip_hash` sticky session upstream balancing to ensure handshake negotiation and persistent socket affinity hit the identical API replica.
- Static assets under `/seed-assets/` are offloaded directly by NGINX disk mapping and must not route through the .NET runtime.

---

## 3. Data Seeding & Testing Guidelines
- For seed pipeline work, use sequential deterministic seeding with realistic skewed distributions (90-9-1 rule or Zipfian distribution) to simulate real-world hot users and contention.
- Include a mandatory validator layer (`Mandatory Validation Gate`) that verifies 100% referential integrity and data constraints before committing snapshots.
- After implementing each seeding step, immediately update its corresponding technical documentation.

---

## 4. Code Quality & Precision Standards
- User prefers analytical, system-level answers with complete listings instead of abbreviations or ellipsis (`...`).
- When updating planning/architecture documents, ensure the full end-to-end process is consistently documented.
- Rollback Strategy: Strangler Fig migrations must follow binary per-slice commits so that any regression can be rolled back cleanly via `git revert <slice-commit>`.
