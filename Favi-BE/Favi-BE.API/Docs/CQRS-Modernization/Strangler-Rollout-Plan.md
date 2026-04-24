# Strangler Rollout Plan

## 1. Rollout mode
Binary per-slice: `implement -> validate runnable state -> merge`.
Rollback: `git revert` về stable commit gần nhất.

## 2. Ordered slices
| Slice | Scope | Success metrics | Exit criteria | Rollback trigger |
|---|---|---|---|---|
| `Foundation.CQRSOutbox` | BuildingBlocks, MediatR, Outbox/Inbox, architecture tests | Build/test baseline, outbox write rate | Build pass + tests pass + atomic outbox | build/test fail, lost event |
| `Auth.LoginCQRS` | Login/Refresh/Logout/Register handlers + controller strangler | auth success rate parity | token format parity + no security regression | login failures spike |
| `Notification.EventDriven` | remove direct notification side effects from write path | duplicate send = 0 | no in-transaction hub push + unread parity | duplicate sends/unread mismatch |
| `Engagement.Commands` | comment/reaction commands | reaction parity, idempotency | parity checks pass | mismatch/duplicates |
| `SocialGraph.Commands` | follow/unfollow/social links + followers/followings ownership | graph parity | follow graph and notification parity | follow mismatch |
| `ContentPublishing.Commands` | post/collection/repost writes | write success parity | content mutation parity | data mismatch |
| `Stories.CommandsAndExpiry` | story commands + expiry process | expiry reliability | parity for expire/cleanup | stale stories leak |
| `Messaging.CQRS` | conversation/message split, realtime hooks | latency/error parity | message-read correctness parity | message ordering/read errors |
| `Moderation.BackofficeCQRS` | report/moderation/admin paths | audit consistency | audit/admin parity | audit integrity issue |

## 3. Mandatory validations each slice
- Build/test pass.
- Backward compatibility validated.
- Outbox/Inbox idempotency validated.
- Architecture boundary tests pass.
- Documentation updated.

## 4. Mandatory parity artifacts each slice
- `old vs new command result parity`.
- `key read-model metrics parity`.