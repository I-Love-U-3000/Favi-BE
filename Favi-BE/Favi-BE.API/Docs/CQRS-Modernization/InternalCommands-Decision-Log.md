# InternalCommands Decision Log

## Decision
- **Status**: `Defer` tại giai đoạn đầu (Slice 0-2).
- **Rationale**: hiện tại chưa có scheduled/deferred command bắt buộc cần persistence-level command queue riêng.

## Review triggers (khi nào adopt)
1. Xuất hiện use-case deferred với SLA rõ (scheduled moderation, delayed publish, retryable long-running business command).
2. Cần exactly-once command execution tách khỏi event handlers.
3. Outbox processor quá tải vì trộn event delivery và deferred workflow.

## If adopted later
- Add table `InternalCommands`:
  - `Id`, `Type`, `Payload`, `EnqueueUtc`, `ProcessAfterUtc`, `Status`, `Retries`, `ProcessedUtc`, `Error`.
- Add hosted processor executing `IMediator.Send(...)`.
- Add idempotency key and retry/poison strategy.

## Risk if deferred
- Một số workflow delayed sẽ tạm xử lý bằng hosted services ad-hoc.
- Cần theo dõi kỹ retry/visibility để tránh hidden failures.

## Risk control while deferred
- Mọi background action phải có telemetry + retry + dead-letter equivalent logging.