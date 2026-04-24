# Read/Write Segregation EFCore -> Dapper Plan

## 1. Contract model per module
- Write contracts: `I<Module>CommandRepository`, `I<Module>UnitOfWork`
- Read contracts: `I<Module>QueryReader`

## 2. Rules
1. Command handlers dùng write DbContext (tracking).
2. Command handlers không gọi `I<Module>QueryReader`.
3. Query handlers dùng `AsNoTracking` và projection DTO.
4. Query handlers không mutate state.

## 3. Phase roadmap
### Phase 1: EF Core read/write split
- Introduce query readers with `AsNoTracking`.
- Keep same DB.

### Phase 2: Hot query projection
- Add read projection tables/materialized views for top latency queries.

### Phase 3: Dapper adoption
- Swap only `I<Module>QueryReader` implementations for selected hot queries.

## 4. Query migration map
| Query Name | Now (`EFCoreAsNoTracking`) | Dapper candidate |
|---|---|---|
| `GetNewsFeedQuery` | EFCore projection | yes (high-volume) |
| `SearchPostsQuery` | EFCore + vector merge | yes |
| `GetNotificationsQuery` | EFCore paging | yes |
| `GetUnreadNotificationCountQuery` | EFCore count | yes |
| `GetConversationsQuery` | EFCore include/projection | yes |
| `GetMessagesQuery` | EFCore paging | yes |
| `GetFollowersQuery` | EFCore join | yes |
| `GetFollowingsQuery` | EFCore join | yes |
| `GetReportsQuery` | EFCore filtering | optional |

## 5. Enforcement
- Add architecture tests:
  - `CommandHandlers` must not reference `QueryReader` namespace.
  - `QueryHandlers` must not reference write repositories/UoW.
- CI gate fail-fast on violation.