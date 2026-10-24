# 02 — Layered Architecture, MediatR Pipeline & Design Patterns (Favi-BE)

## 1. Layered Structure Within Modules (Onion / Hexagonal Architecture)

Every module in Favi-BE is structured according to an encapsulated **Onion Architecture**:

```
┌────────────────────────────────────────────────────────┐
│                      Infrastructure                    │
│   EF Core Configurations, Adapters, Outbox/Inbox IO    │
│  ┌──────────────────────────────────────────────────┐  │
│  │                    Application                   │  │
│  │    Commands, Queries, Handlers, Validators, DTOs │  │
│  │  ┌────────────────────────────────────────────┐  │  │
│  │  │                   Domain                   │  │  │
│  │  │  Aggregates, Entities, Value Objects,      │  │  │
│  │  │  Domain Events, Business Invariants        │  │  │
│  │  └────────────────────────────────────────────┘  │  │
│  └──────────────────────────────────────────────────┘  │
└────────────────────────────────────────────────────────┘
```

### 1.1 Domain Layer
- Zero dependencies on Entity Framework, ASP.NET Core, or SQL abstractions.
- Contains Aggregate Roots inheriting from `Entity<TId>` and enforces business invariants via `CheckRule(new SomeBusinessRule(...))`.
- All domain state mutations must occur strictly through Aggregate methods (e.g., `post.UpdateCaption(...)`, `reaction.Toggle(...)`); public property setters are prohibited.
- Emits Domain Events (`IDomainEvent`) when critical business domain state changes.

### 1.2 Application Layer
- Implements CQRS via MediatR:
  - **Commands**: Inherit `ICommand<TResult>` (or `IRequest<TResponse>`). Represent state-mutating operations.
  - **Queries**: Inherit `IQuery<TResult>`. Read-only, side-effect-free data retrieval operations.
- Contains returned DTOs (Data Transfer Objects) and ViewModels.
- Validates incoming command/query payloads using **FluentValidation** (`AbstractValidator<TRequest>`).
- Defines Ports (Interfaces) for external dependencies (e.g., `IAuthWriteRepository`, `ICommentQueryReader`).

### 1.3 Infrastructure Layer
- Implements the Ports defined in the Application layer.
- Configures EF Core Fluent API mappings (`IEntityTypeConfiguration<TEntity>`) targeting PostgreSQL tables.
- Implements persistence repositories, Outbox event publishers, and Inbox message consumers.

---

## 2. Thin API Host (`Favi-BE.API`) & Pipeline Behaviors

The `Favi-BE.API` project serves strictly as the Composition Root and HTTP host. Controllers are deliberately thin, translating HTTP requests into MediatR Commands/Queries and mapping results to HTTP responses:

```csharp
[HttpPost]
public async Task<IActionResult> CreatePost([FromBody] CreatePostRequest request, CancellationToken ct)
{
    var command = new CreatePostCommand(User.GetUserId(), request.Caption, request.MediaUrls, request.Tags);
    var result = await _mediator.Send(command, ct);
    return CreatedAtAction(nameof(GetPostById), new { id = result.Id }, result);
}
```

### 2.1 MediatR Pipeline Behaviors

Every request dispatched through `_mediator.Send()` traverses an ordered pipeline of cross-cutting behaviors:

```
HTTP Request 
     │
     ▼
┌─────────────────────────┐
│   ValidationBehavior    │ ──▶ Executes FluentValidation; throws ValidationException on errors
└────────────┬────────────┘
             │
             ▼
┌─────────────────────────┐
│     LoggingBehavior     │ ──▶ Logs Request name, CorrelationId, UserId, and sanitized payload
└────────────┬────────────┘
             │
             ▼
┌─────────────────────────┐
│   PerformanceBehavior   │ ──▶ Tracks elapsed execution time; logs warnings if execution > 500ms
└────────────┬────────────┘
             │
             ▼
┌─────────────────────────┐
│   TransactionBehavior   │ ──▶ (Applied only to ICommand) Opens EF Transaction, commits on success,
└────────────┬────────────┘     rolls back automatically on unhandled exceptions
             │
             ▼
┌─────────────────────────┐
│     Command / Query     │ ──▶ Executes core business logic inside the Handler
│         Handler         │
└─────────────────────────┘
```

---

## 3. Dependency Injection Management (DI Composition)

To prevent `Program.cs` from becoming a cluttered "God Class" with hundreds of sprawling service registrations, dependencies are strictly modularized:

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// 1. Core infrastructure & shared kernels
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddAuthentication(builder.Configuration);
builder.Services.AddApplication(builder.Configuration);

// 2. Individual isolated modules
builder.Services.AddAuthModule();
builder.Services.AddSocialGraphModule();
builder.Services.AddContentPublishingModule();
builder.Services.AddContentDiscoveryModule();
builder.Services.AddEngagementModule();
builder.Services.AddNotificationsModule();
builder.Services.AddStoriesModule();
builder.Services.AddMessagingModule();
builder.Services.AddModerationModule();

var app = builder.Build();
app.UseStartupTasks(); // Run migrations and health checks
```

> **Mandatory Rule**: Never register module-internal repositories or services directly in `Program.cs`. Each module must expose a dedicated extension method (e.g., `AddAuthModule.cs`) to encapsulate and register its own dependencies.

---

## 4. Strangler Fig Pattern & Rollback Strategy

Favi-BE applies the **Strangler Fig Pattern** to migrate legacy monolith services incrementally to modern CQRS modules without system downtime:

```
[ Phase 1: Parallel Deployment of New Handler ]
Controller ──▶ _mediator.Send(Command) ──▶ Modular Handler
                (Legacy IAuthService retained in DI as a safe rollback fallback)

[ Phase 2: Parity Signoff ]
Once automated integration tests and load tests prove 100% parity and stability,
remove DI registration for legacy services.

[ Phase 3: Cleanup ]
Delete legacy code paths and deprecated intermediate tables/columns.
```

### Rollback Contract
- Each migration slice is packaged into **exactly 1 independent Git Commit**.
- If a production defect or regression is detected, rollback is achieved with a single command:
  ```bash
  git revert <slice-commit-hash>
  ```
  This immediately restores previous routing and registrations without chaotic manual code editing during an incident.
