# Copilot Instructions
You have to work as excellent with pure exact because I will use Claude Opus 4.6 to review your works. 

## Project Guidelines
- User prefers analytical, system-level answers and explicitly does not want file creation or code modifications unless requested.
- User prefers not to run builds for changes that only modify Markdown/planning documents and do not affect code.
- When updating planning documents, avoid partial additions that create numbering duplication; document the full end-to-end process consistently across all steps.
- User wants absolute precision in planning docs and prefers full explicit listings instead of ellipsis (no abbreviated '...') when documenting inventory/details.
- User prefers a detailed architecture migration plan to CQRS + Outbox + MediatR with clear bounded contexts, aggregate roots, command/query mapping, schema redesign, folder restructuring, SignalR notification integration via MediatR notifications, and phased strangler-pattern rollout. The CQRS plan should explicitly include auth commands (especially Login) and separate read/write interfaces early: write via EF Core tracking, read via EF Core AsNoTracking first, with a later migration path to Dapper for reads. Additionally, for CQRS strangler planning docs, user prefers binary per-slice rollout (implement -> validate -> merge), rollback via git revert, no traffic-percentage rollout, and no feature-flag/toggle naming requirements.
- User wants plan/checklist to explicitly specify, for each section/slice, which files must be read and which are priority references before implementation.
- For every slice implementation request, user requires complete adherence to all stated requirements: read all relevant tasks/docs, reference all mandatory materials, and strictly follow instructions before coding.
- Dependency Injection Pattern: DO NOT add raw service registrations directly to `Program.cs`. Instead, use `IServiceCollection` extension methods grouped by responsibility (for example `AddInfrastructure`, `AddAuthModule`). Each module must have its own extension method to maintain encapsulation.

## Data Seeding Guidelines
- For seed pipeline work, use sequential deterministic seeding with realistic skewed models (e.g., 90-9-1 or Zipf).
- Include a mandatory validator layer that validates all seeded data before proceeding.
- After implementing each seeding step, immediately add/update its corresponding step document so implementation and documentation stay in sync.