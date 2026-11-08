# Pilot baseline: evaluator khớp data gate

08/11/2026. Offline adapter/evaluator đã chạy main và sparse pilot.
Primitive parity checkpoint: `6c2083e`. Xem [contract](../ADAPTIVE_OFFLINE_CONTRACT.md).
Candidate/target hashes khớp giữa methods và cohort denominators khớp data gate.

| Scenario | Method | Test users | nDCG@10 |
|---|---|---:|---:|
| Main | Popularity | 4916 | 0,00375155 |
| Main | Topic-only | 4916 | 0,01234318 |
| Main | Formula + cold fallback | 4916 | 0,00539433 |
| Sparse-topic | Popularity | 4910 | 0,00464461 |
| Sparse-topic | Topic-only | 4910 | 0,00442468 |
| Sparse-topic | Formula + cold fallback | 4910 | 0,00427118 |

Main topic-only dẫn; sparse Popularity dẫn. Chỉ pilot descriptive, chưa phải
superiority hoặc causal evidence. Informative preferences và exposure/labels
đều liên quan latent interests: synthetic setup cố ý thuận lợi cho onboarding.
Formula nhận comments/follows/tag-count, topic-only nhận declared preferences,
Popularity chỉ likes. Đây là system track khác tín hiệu, chưa có router hoặc
likes-only collaborative results.

101 full Python tests passed, rồi thêm bounded-pool test: 4 adapter tests pass.
Primitive milestone trước có 524 C#/Python checks và 13 .NET ColdStart tests pass.
Không có HTTP/PostgreSQL mới. Không thay đổi kết quả legacy.

[Main JSON](adaptive-baselines-main-20261108.json),
[Sparse JSON](adaptive-baselines-sparse-20261108.json) giữ full precision/gate/cohorts.
Raw: `.tools/benchmarks/adaptive-baselines-{main|sparse}-20261108-001`, có source
snapshots, manifests và user-level predictions. Bước tiếp theo là freeze protocol,
search grid và runner collaborative/router trước confirmatory seeds.
