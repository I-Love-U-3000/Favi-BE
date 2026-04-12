# Copilot Instructions

## Project Guidelines
- User prefers analytical, system-level answers and explicitly does not want file creation or code modifications unless requested.
- When updating planning documents, avoid partial additions that create numbering duplication; document the full end-to-end process consistently across all steps.

## Data Seeding Guidelines
- For seed pipeline work, use sequential deterministic seeding with realistic skewed models (e.g., 90-9-1 or Zipf).
- Include a mandatory validator layer that validates all seeded data before proceeding.
- After implementing each seeding step, immediately add/update its corresponding step document so implementation and documentation stay in sync.