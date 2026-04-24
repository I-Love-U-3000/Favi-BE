# Inbox Implementation Guide

## 1. Scope
Đảm bảo consume integration events idempotent khi retry/replay.

## 2. Data model
Use `InboxMessages` table with unique `(MessageId, Consumer)`.

## 3. Consume flow
1. Receive message from outbox/event bus.
2. Upsert inbox record by `(MessageId, Consumer)`.
3. Nếu record đã `Processed` -> skip (idempotent return).
4. Execute handler.
5. Success -> mark `Processed`, set timestamp.
6. Failure -> increment retry + error.
7. Over max retry -> `Poisoned` and alert.

## 4. Handler rules
- Handler phải idempotent theo business key.
- Không assume exactly-once delivery.
- Side effects external phải kiểm tra dedup key.

## 5. Operational controls
- Retry policy: exponential backoff.
- Poison queue/dashboard for manual replay.
- Replay command chỉ xử lý messages `Poisoned` hoặc `Failed`.

## 6. Acceptance tests
- Same message delivered nhiều lần chỉ tạo 1 side effect.
- Failure giữa chừng không gây duplicate update.
- Replay sau failure cho kết quả deterministic.