# 🎯 Why Can't You Scroll Beyond Page N?

## The Issue in 30 Seconds

```
GetFeedAsync:
  skip: 0  ← Always 0!
  take: 500 ← Always 500!

For ALL pages, you fetch the SAME 500 posts from DB.
Then paginate those 500 in-memory.

If you have 1000 posts in your feed:
  Page 1-25 ✅ Shows posts 1-500
  Page 26+  ❌ Shows posts 501-1000 → Don't exist in the 500 fetched!
```

---

## Visual Proof

```
REQUEST #1: GET /api/posts/feed?page=1&pageSize=20
    ↓
GetFeedPagedAsync(userId, skip=0, take=500)
    ↓
DB: SELECT * FROM Posts WHERE (owner=userId OR follow) 
    ORDER BY CreatedAt DESC
    LIMIT 500
    ↓
    Returns: [Post 1000-500]  ← Most recent 500
    ↓
Filter + Score in memory
    ↓
Paginate: skip=0, take=20
    ↓
RETURN: [Post 1000-981] ✅ Works!

REQUEST #2: GET /api/posts/feed?page=25&pageSize=20
    ↓
GetFeedPagedAsync(userId, skip=0, take=500)  ← Same call!
    ↓
DB: Returns SAME [Post 1000-500]  ← Same 500!
    ↓
Filter + Score in memory
    ↓
Paginate: skip=480, take=20
    ↓
RETURN: [Post 520-501] ✅ Still works (within 500)

REQUEST #3: GET /api/posts/feed?page=26&pageSize=20
    ↓
GetFeedPagedAsync(userId, skip=0, take=500)  ← Still same!
    ↓
DB: Returns SAME [Post 1000-500]  ← Can't go below 500!
    ↓
Filter + Score in memory (still 500 max)
    ↓
Paginate: skip=500, take=20
    ↓
RETURN: [] ❌ Empty! Can't access Post 500-1!
```

---

## Code Evidence

### PostService.cs (Line 76-79)
```csharp
var (candidates, _) = await _uow.Posts.GetFeedPagedAsync(
    currentUserId,
    skip: 0,                      // ⚠️ HARDCODED ZERO
    take: TrendingCandidateLimit  // ⚠️ HARDCODED 500
);
```

### PostRepository.cs (Line 132-149)
```csharp
public async Task<(IEnumerable<Post> Items, int Total)> GetFeedPagedAsync(Guid profileId, int skip, int take)
{
    var baseQuery = _dbSet
        .Where(p => /* privacy */ && !archived && !deleted)
        .OrderByDescending(p => p.CreatedAt);

    var total = await baseQuery.CountAsync();

    var items = await baseQuery
        .Skip(skip)      // ← skip parameter (but always 0 from service!)
        .Take(take)      // ← take parameter (but always 500 from service!)
        .ToListAsync();

    return (items, total);
}
```

**The repository CAN handle pagination, but the service never uses it!**

---

## Real Data Example

**Your Feed:**
- You: 5 posts
- Follow 13 people
- They have: 400+ posts total
- **Total in feed: ~405 posts**

**Can you see them all?**
```
No. Only top 500 most recent.
Fortunately, 405 < 500, so you CAN see all 405. ✅

But if those 13 people had posted 600+ times:
→ You'd only see 500
→ 100+ posts hidden forever
→ Can't scroll past 25 pages ❌
```

---

## Data Flow Diagram

```
┌──────────────────────────────┐
│ DB: User's Feed (1000 posts) │
│ Post #1000 (newest)          │
│ Post #999                    │
│ ...                          │
│ Post #501                    │
│ Post #500                    │
│ Post #499 (oldest visible)   │
│ Post #498 (not fetched)      │
│ ...                          │
│ Post #1 (oldest)             │
└──────────────────────────────┘
           ↓
    GetFeedPagedAsync
    skip=0, take=500
           ↓
┌──────────────────────────────┐
│ Memory: 500 Posts            │
│ [Post 1000-501]              │
│ Scored + Sorted              │
└──────────────────────────────┘
           ↓
    Paginate in-memory
           ↓
┌──────────────────────────────┐
│ Page 1: [1-20]     ✅        │
│ Page 2: [21-40]    ✅        │
│ ...                         │
│ Page 25: [481-500] ✅        │
│ Page 26: [] ❌               │
│ (Posts 501-1000 hidden!)    │
└──────────────────────────────┘
```

---

## The "Why"

Engineers probably did this for **performance**:

```
Trending Score for 10,000 posts = Slow ❌
Trending Score for 500 posts = Fast ✅

Trade-off:
- GAIN: Faster algorithm (~500ms vs ~5s)
- LOSS: Can't scroll past 500 posts
```

They chose **speed over features**.

---

## The Fix (15 lines of code)

```csharp
// Before:
public async Task<PagedResult<PostResponse>> GetFeedAsync(Guid currentUserId, int page, int pageSize)
{
    var (candidates, _) = await _uow.Posts.GetFeedPagedAsync(
        currentUserId,
        skip: 0,                      // ← Hardcoded
        take: TrendingCandidateLimit
    );
    // ...
}

// After:
public async Task<PagedResult<PostResponse>> GetFeedAsync(Guid currentUserId, int page, int pageSize, int offset = 0)
{
    var (candidates, _) = await _uow.Posts.GetFeedPagedAsync(
        currentUserId,
        skip: offset,                 // ← Parameterized!
        take: TrendingCandidateLimit
    );
    // ...
}

// API: GET /api/posts/feed?page=1&pageSize=20&offset=0
// Then offset=500 for next batch, offset=1000, etc.
```

---

## TL;DR

| What | Value | Issue |
|------|-------|-------|
| **Candidate limit** | 500 | Max 500 posts per request |
| **Skip parameter** | 0 (hardcoded) | Always fetches same 500 |
| **Actual feed size** | 405 (your case) | Fine, < 500 |
| **Can scroll past 25 pages?** | ❌ No | Max 500 / 20 = 25 pages |
| **If followees posted 600+ times** | ❌ Problem | 100+ posts hidden |

**Status**: Works now (your posts < 500) ✅  
**Limitation**: Can't infinite scroll (by design) ❌  
**Fix effort**: 5 minutes 🚀
