# Microservice Migration Checklist (Execution-ready)

> Dùng checklist này để tracking theo tuần. Chỉ tick khi có artifact chứng minh.

## A. Phase 0 — Baseline stabilization
- [ ] Khóa `seed key`, dataset, docker resource allocation.
- [ ] Chạy full benchmark baseline (`smoke/sanity/load/stress-spike-soak`).
- [ ] Lưu report before-split (latency/error/throughput).
- [ ] Fix known race critical path (reaction duplicate key case).
- [ ] Chuẩn hóa error handling (không lộ raw DB exception ra API).

Evidence:
- [ ] `test-results/*`
- [ ] `progress-report` update
- [ ] commit hash baseline

## B. Phase 1 — Modular monolith boundaries
- [ ] Định nghĩa module map: Identity/SocialGraph/Content/Engagement/FeedQuery/Notification/Search.
- [ ] Chốt và review diagram boundary (`domain-boundary-map-v1.md` Diagram A/B).
- [ ] Tách application services theo module.
- [ ] Cấm direct repository access xuyên module (chỉ qua contracts).
- [ ] Kiểm tra dependency graph không vòng lặp nguy hiểm.

Evidence:
- [ ] boundary diagram
- [ ] dependency report
- [ ] integration tests pass

## C. Phase 2 — Event backbone
- [ ] Tạo outbox table + publisher worker.
- [ ] Tạo event schema `*.v1` có `eventId`, `occurredAt`, `aggregateId`.
- [ ] Thiết lập message bus (Kafka/Redis Streams).
- [ ] Tạo idempotent consumer + dead-letter handling.
- [ ] Tạo replay tool (rebuild projection).

Evidence:
- [ ] event catalog file
- [ ] retry/replay demo logs
- [ ] queue lag metric dashboard

## D. Phase 3 — Extract Fan-out worker
- [ ] Chuyển notification side-effects ra worker.
- [ ] Chuyển vector indexing side-effects ra worker.
- [ ] Monolith publish event thay vì gọi trực tiếp side-effects.

Evidence:
- [ ] worker service health endpoints
- [ ] no direct call path from write -> side-effects
- [ ] spike test noti storm results

## E. Phase 4 — Extract Read API service
- [ ] Tách feed/profile/read endpoints sang Read service.
- [ ] Route đọc qua gateway.
- [ ] Đọc từ read replica.
- [ ] Redis cache cho read hotspots.
- [ ] SLA read-after-write định nghĩa rõ (eventual window).
- [ ] Validate topology theo `domain-boundary-split-plan-v1.md` Diagram 1.

Evidence:
- [ ] gateway routing table
- [ ] cache hit ratio report
- [ ] P95 read improvements vs baseline

## F. Phase 5 — Extract Write API service
- [ ] Tách write endpoints sang Write service.
- [ ] Write service là owner của tables write.
- [ ] Publish events 100% qua outbox.
- [ ] Integration/E2E pass qua gateway.
- [ ] Validate sync/async flow theo `domain-boundary-split-plan-v1.md` Diagram 3.

Evidence:
- [ ] contract tests
- [ ] full E2E report
- [ ] rollback plan validated

## G. Phase 6 — Scale hardening
- [ ] HPA/replica strategy theo service role (Read/Write/Worker).
- [ ] Rate limit theo route nóng.
- [ ] Circuit breaker + retry budgets cho dependencies.
- [ ] SLO dashboards + alerts production-like.

Evidence:
- [ ] stress/spike/soak after-split report
- [ ] capacity recommendation doc

---

## H. k6 migration checklist (sau split)
- [ ] Tạo version test suite route qua gateway.
- [ ] Update `common.js` base routes cho read/write intent.
- [ ] Giữ cùng seed data + same scenario intensity.
- [ ] So sánh before/after bằng cùng tiêu chí.

Outputs bắt buộc:
- [ ] `before-vs-after-summary.md`
- [ ] percentile deltas (p50/p95/p99)
- [ ] error-rate deltas
- [ ] throughput deltas

---

## I. Sign-off criteria
- [ ] Không regression chức năng chính (functional + integration + e2e pass).
- [ ] Read path tốt hơn baseline đã chốt.
- [ ] Không mất event dưới load spike.
- [ ] Không vi phạm data integrity dưới stress hotspots.
- [ ] Có rollback plan kiểm thử được.
