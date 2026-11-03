# Copilot & Agent Operating Instructions

You must work with exceptional precision and system-level depth because GPT Codex and rigorous code review tools will inspect and validate your output.

---

## 1. Mandatory Documentation Protocol

### 1.1 Before Starting Any Task (Search & Review First)
- **Mandatory Document Search**: Before modifying any code, creating new files, or proposing architectural changes, the agent **MUST search for and read relevant technical documents** in the `document/` folder (`Favi-BE/document` and/or `Favi-FE/document`).
- Review the specific module boundaries, aggregate roots, layer responsibilities, idempotency requirements, NGINX routing rules, and database invariants established for the affected areas.
- Never write code based on assumptions if a document exists covering that domain.

### 1.2 Undocumented Technical Terms & Patterns Protocol (Proactive Documentation)
- **Mandatory Documentation of New / Discovered Concepts**: If the agent implements, updates, discovers, or encounters any technical terms, design patterns, architectural mechanisms, database conventions (e.g., table record versioning, optimistic locking, advisory locks, specific indexing strategies, cache topologies), or algorithms that are **not yet documented** or are insufficiently documented in `document/`, the agent **MUST proactively create new documentation or update existing documents** covering those terms and concepts.
- No technical term, pattern, or mechanism should exist in the codebase without a formal, comprehensive technical specification in `document/`.

### 1.3 After Making Any Changes (Keep Documents Synchronized)
- **Mandatory Documentation Update**: After implementing, refactoring, or modifying any feature, architecture, schema, API endpoint, or data flow, the agent **MUST immediately update the corresponding documents** in `document/` to reflect the changes.
- Implementation and documentation must stay 100% in sync at all times. Leaving documents stale or outdated is strictly forbidden.

---

## 2. Core Architectural & Implementation Principles

### 2.1 Backend Architecture (Modular Monolith & DDD)
- **Physical Isolation**: Keep modules decoupled. No direct cross-module assembly references between module projects.
- **No Shared Entities**: Each module owns its aggregates and database tables. Never query another module's DbContext or internal tables directly.
- **Cross-Module Communication**:
  - Asynchronous / Side-effects: Must use **Integration Events** dispatched via the **Outbox/Inbox pattern**.
  - Synchronous Queries / Reads: Must use **Application Facades** or dedicated **Query Reader Contracts** registered at the host level.
- **Dependency Injection**: **DO NOT** add raw service registrations directly to `Program.cs`. Instead, encapsulate them inside `IServiceCollection` extension methods grouped by responsibility (e.g., `AddInfrastructure`, `AddAuthModule`, `AddNotificationsModule`). Each module must have its own extension method.
- **CQRS & Clean Architecture**: Domain encapsulates invariants; Application defines Commands/Queries/Ports; Infrastructure implements them; API Host is thin. Commands use EF Core tracking; Queries use `.AsNoTracking()` (or Dapper).
- **Dual-Layer Idempotency**: All mutating HTTP requests require `Idempotency-Key` (enforced via `IdempotencyMiddleware`). Event consumers must use `IInbox.TryStartProcessingAsync(messageId, consumerName)`.
- **NGINX Gateway & SignalR**: Respect NGINX routing, rate limits, and `ip_hash` sticky sessions for WebSockets.

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

---

## 5. Git Hygiene & Plan File Tracking Protocol
- **Strictly Exclude `.md` Plan Files from Git**: All `.md` plan files (including implementation plans, task breakdown plans, architectural scratchpads, `*plan*.md`, or agent planning notes) **MUST NEVER be tracked in Git, committed, or pushed to the remote GitHub repository**.
- **Distinction from Technical Documentation**: Only formal, long-lived system specifications designated for the `document/` folder belong in version control. Transient or task-oriented `.md` plan files are strictly local working artifacts.
- **Staging Verification & Prevention**:
  - Never stage plan files (`git add <file>.md` for plans is strictly prohibited).
  - Before executing any commit, verify via `git status` that no `.md` plan files are staged. If any plan file is inadvertently staged, immediately unstage it using `git restore --staged <file>` or `git reset HEAD <file>`.
  - Maintain entries in `.gitignore` or `.git/info/exclude` (e.g., `*plan*.md`, `*.plan.md`) to prevent accidental tracking.

