# 06 — Version Control (Git), Database Migrations & Seed Pipeline (Favi-BE)

## 1. Version Control & Git Strategy

The Favi-BE codebase adheres to standardized version control protocols ensuring stability and complete traceability:

### 1.1 Conventional Commits Specification
Every commit message must clearly state the change type and affected scope:
- `feat(auth): add auth session aggregate and query reader`
- `fix(engagement): prevent race condition on comment reaction toggle`
- `refactor(socialgraph): extract follow relations into dedicated module`
- `test(e2e): add k6 scenario for feed scrolling pagination`
- `docs(arch): update bounded contexts dependency matrix`

### 1.2 Binary Rollback Contract
- When migrating business slices via the Strangler Fig pattern, **each complete slice must reside in exactly one Git Commit**.
- **Rollback Rule**: In the event post-deployment monitoring detects a regression, rollback is executed via a single command:
  ```bash
  git revert <slice-commit-hash>
  ```
  This immediately re-enables legacy DI registrations and routing cleanly without rushed, error-prone production patching.

---

## 2. Database Schema Evolution (EF Core Migrations)

### 2.1 Zero-Downtime Schema Evolution Rules
When evolving production database schemas:
1. **Additive Only Changes**:
   - Add new nullable columns (`NULL`) or columns with safe defaults (`DEFAULT`).
   - Create new tables.
   - Immediate renaming or dropping of columns is prohibited while legacy application versions are running.
2. **Three-Phase Pattern for Renaming / Dropping Columns**:
   - *Phase 1 (Expand)*: Create new column; deploy code that dual-writes to both old and new columns.
   - *Phase 2 (Migrate)*: Run backfill scripts to synchronize historic records from old to new column; shift all read operations to the new column.
   - *Phase 3 (Contract)*: After stability signoff, deploy a migration that drops the deprecated old column.

### 2.2 Common EF Core CLI Commands
Run within `Favi-BE/Favi-BE`:
```bash
# Add new migration for DbContext
dotnet ef migrations add <MigrationName> --project Favi-BE.API --startup-project Favi-BE.API

# Update database directly (development/staging)
dotnet ef database update --project Favi-BE.API --startup-project Favi-BE.API

# Generate idempotent SQL script for DBA review prior to production deployment
dotnet ef migrations script --idempotent --output migration.sql --project Favi-BE.API
```

### 2.3 Multi-Replica Startup Advisory Lock (Cluster Startup Coordination)
In Docker Compose environments comprising multiple API replicas (`favi-api-1`, `favi-api-2`), cold-starts or fresh deployments (`docker-compose down -v && docker-compose up -d`) trigger simultaneous invocations of `ApplyDatabaseMigrationsAndSeedAsync()`.

To prevent race conditions, duplicate seed records, and foreign key deadlocks:
- The system employs a **PostgreSQL Session-Level Advisory Lock** (`SELECT pg_advisory_lock(742189);`) implemented in [`StartupTasksExtensions.cs`](file:///c:/Users/MINH%20QUANG/Favi/Favi-BE/Favi-BE/Favi-BE.API/DependencyInjection/StartupTasksExtensions.cs).
- The first container to acquire the lock executes the migration pipeline (including [`20260423000000_AddUpdatedAtAndVersionToProfile`](file:///c:/Users/MINH%20QUANG/Favi/Favi-BE/Favi-BE/Favi-BE.API/Migrations/20260423000000_AddUpdatedAtAndVersionToProfile.cs) adding `UpdatedAt` and `Version` to `Profiles`) and completes the Seed Pipeline.
- The secondary container is held at the lock boundary; once released (`SELECT pg_advisory_unlock(742189);`), it checks that migrations are current and seed data is present (`hasExistingProfiles == true`), automatically bypasses seed generation, and begins accepting live traffic.

---

## 3. Optimistic Concurrency Control & Record Versioning

To prevent data corruption caused by concurrent overwrites (**Lost Update Problem**) when multiple clients or processes concurrently modify the same entity (e.g., editing user profiles, updating post captions, modifying collection metadata):

### 3.1 Database Column Conventions
Entity tables subject to concurrent modifications (`Profiles`, `Posts`, `Collections`, `Comments`, `Stories`, `Messages`, `Conversations`, `Reposts`) standardize on two technical columns:
- **`Version` (`integer NOT NULL DEFAULT 1`)**: Stores the current monotonically increasing revision number.
- **`UpdatedAt` (`timestamp with time zone NOT NULL DEFAULT NOW()`)**: Tracks the exact timestamp of the latest mutation.

### 3.2 Application-Level Concurrency Verification Workflow
Within the CQRS command pipeline, version verification operates as follows:

```
[ Client ]                           [ Command Handler ]                   [ Database ]
    │                                         │                                  │
    │── 1. Query Data (includes Version = N) ─▶│                                 │
    │◀─ Returns Dto (Version = N) ────────────│                                  │
    │                                         │                                  │
    │── 2. Send UpdateCommand(Version = N) ──▶│                                  │
    │                                         │── 3. FindByIdAsync() ───────────▶│
    │                                         │◀─ Entity(Version = M) ───────────│
    │                                         │                                  │
    │                                         ├── M != N? ──▶ Return 409 Conflict│
    │                                         │               (CONCURRENCY_      │
    │                                         │                CONFLICT)         │
    │                                         │                                  │
    │                                         └── M == N? ──▶ Incr Version = N+1 │
    │                                                         UpdatedAt = Now()  │
    │                                                         Save to DB ───────▶│
    │◀─ 4. Success Response (200 OK) ─────────┴──────────────────────────────────┘
```

1. **Query**: Client queries the resource and receives a DTO containing the current `Version` (e.g., `Version = 1`).
2. **Dispatch Mutation**: When sending an update command (e.g., [`UpdateProfileCommand`](file:///c:/Users/MINH%20QUANG/Favi/Favi-BE/Favi-BE/Favi-BE.Modules.Auth/Application/Commands/UpdateProfile/UpdateProfileCommandHandler.cs), `UpdatePostCommand`, `UpdateCollectionCommand`), the client transmits its known `Version`.
3. **Version Check**:
   ```csharp
   if (request.Version.HasValue && request.Version.Value != entity.Version)
   {
       return CommandResult.Fail("CONCURRENCY_CONFLICT", 
           "The record was modified in another session. Please refresh and try again.");
   }
   ```
4. **Increment & Commit**: If versions match, the handler applies updates, increments `Version = Version + 1`, and sets `UpdatedAt = DateTime.UtcNow`.
5. **Client Response**: If conflict is detected, controllers map `CONCURRENCY_CONFLICT` to HTTP **`409 Conflict`**, prompting the client application to refresh its view before re-submitting changes.

### 3.3 Database-Level Concurrency Tokens (EF Core Roadmap)
Complementing application handler checks, entities configured with EF Core Concurrency Tokens enforce database-level validation:
```csharp
builder.Entity<Profile>()
    .Property(p => p.Version)
    .IsConcurrencyToken();
```
EF Core generates SQL statements formatted as `WHERE "Id" = @id AND "Version" = @oldVersion`. If the row was modified concurrently, zero rows are affected and EF Core throws `DbUpdateConcurrencyException`, protecting database integrity even against external raw SQL updates.

---

## 4. Deterministic Seed Pipeline & Test Data Architecture

To support high-concurrency stress testing, realistic UI testing, and benchmarks, the system includes a **Deterministic Seed Pipeline**:

```
[ Step 0: Constants & Context ] ──▶ Read configurations, initialize deterministic PRNG
              │
              ▼
[ Step 1: Users & Profiles ]    ──▶ Generate test profiles with realistic avatar distributions
              │
              ▼
[ Step 2: Social Graph ]        ──▶ Establish Follow relationships using Power-Law (Zipf)
              │
              ▼
[ Step 3: Posts & Media ]       ──▶ Insert posts linked to static assets in /seed-assets/
              │
              ▼
[ Step 4: Engagement ]          ──▶ Generate hierarchical comment trees and reaction distributions
              │
              ▼
[ Step 5: Tags & Collections ]  ──▶ Associate hashtags and curated collections
              │
              ▼
[ Step 6: Notifications ]       ──▶ Seed notification inboxes across accounts
              │
              ▼
[ Step 7: Stories ]             ──▶ Create active (< 24h) and expired stories
              │
              ▼
[ Mandatory Validation Gate ]   ──▶ Validate referential integrity, counts & FK consistency
```

### 4.1 Realistic Data Distribution Models (90-9-1 Rule & Zipfian Power-Law)
Rather than naive uniform distributions, seed data simulates actual social dynamics:
- 1% of accounts (Influencers/Creators) hold 90% of followers and engagement.
- 9% of accounts are regular active participants.
- 90% of accounts are passive consumers (lurkers).
This exposes the database to realistic "Hot User / Hot Post" row-locking contention and hotspot indexing scenarios during benchmark runs.

### 4.2 Mandatory Validation Gate
Prior to completing the seed routine, an automated integrity suite validates:
- Zero orphan records with invalid foreign keys.
- Every post links to at least one valid media attachment.
- Follower/following counter denormalizations on `profiles` match table counts in `follows` with 100% precision.
