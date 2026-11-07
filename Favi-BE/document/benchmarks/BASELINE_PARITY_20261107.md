# Baseline ranking parity checkpoint

07/11/2026: **524 checks PASS**, 20 warm and 25 cold fixtures. Existing .NET
ColdStart suite: **13 passed**, none failed. These are primitive ranking checks,
not production end-to-end equivalence.

`run_ranker_parity.py` links actual production ColdStartRanker.cs and extracts
warm score/damping blocks from the adapter fail-closed into a temporary .NET
project. Python `favi_rankers.py` must match all IDs and base scores within
1e-12 relative/absolute tolerance. Fixtures use seed 20261107 and cover
follow/mutual/self, tag cap, interaction cap, past/future ages, stable input
ties, author damping, cold dedup, case-insensitive topics, empty GUID, absent
topics, zero/short/large limit and general fallback. No claim for untested
nonfinite inputs or full GUID boundary ordering.

Raw: `.tools/benchmarks/ranker-parity-20261107-001`; includes source hashes,
fixtures, generated C# and stdout/stderr. Re-run with a new output:

```powershell
./.tools/oasis-env/Scripts/python.exe tools/oasis-smoke/run_ranker_parity.py --output .tools/benchmarks/ranker-parity-new
dotnet test Favi-BE.UnitTests/Favi-BE.UnitTests.csproj --no-restore --filter 'FullyQualifiedName~ColdStart' --verbosity quiet
```

No production code changed. Legacy benchmark contract stays unchanged: zero
tags, source-ID ties, own/prior-positive exclusion; production stable ties
depend on candidate input order and tag term counts tags rather than interest
overlap. Pure parity excludes SQL retrieval, permissions, hydration, HTTP and
reaction history construction. Cold reader offline pool adapter remains to
implement before freezing the adaptive protocol.

Tests pass with pre-existing NU1903 SignalR.Common 9.0.11, EF relational version
conflict and compiler warnings. Dependency upgrades are outside this checkpoint.
