# Copilot Instructions

## Project Guidelines
- User prefers analytical, system-level answers and explicitly does not want file creation or code modifications unless requested.
- When updating planning documents, avoid partial additions that create numbering duplication; document the full end-to-end process consistently across all steps.
- User prefers a detailed architecture migration plan to CQRS + Outbox + MediatR with clear bounded contexts, aggregate roots, command/query mapping, schema redesign, folder restructuring, SignalR notification integration via MediatR notifications, and phased strangler-pattern rollout. The CQRS plan should explicitly include auth commands (especially Login) and separate read/write interfaces early: write via EF Core tracking, read via EF Core AsNoTracking first, with a later migration path to Dapper for reads. Additionally, for CQRS strangler planning docs, user prefers binary per-slice rollout (implement -> validate -> merge), rollback via git revert, no traffic-percentage rollout, and no feature-flag/toggle naming requirements.

## Data Seeding Guidelines
- For seed pipeline work, use sequential deterministic seeding with realistic skewed models (e.g., 90-9-1 or Zipf).
- Include a mandatory validator layer that validates all seeded data before proceeding.
- After implementing each seeding step, immediately add/update its corresponding step document so implementation and documentation stay in sync.