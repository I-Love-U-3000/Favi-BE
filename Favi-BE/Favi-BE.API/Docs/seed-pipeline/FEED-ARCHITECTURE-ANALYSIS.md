# 📋 Feed Architecture: Current vs Ideal

## Your Question Decoded

> "Dữ liệu bạn seed toàn 4/10/2026 (fresh), vậy lý thuyết FE implement chuẩn có thể scroll được hết không? Hay hạn chế ở service layer?"

**Answer**: ✅ Data is fresh (4/10/2026), but ❌ Service layer **limits scrolling to 500 posts only**.

---

## Current Architecture (Broken)

### Code:
```csharp
// Line 76-80: PostService.GetFeedAsync
var (candidates, _) = await _uow.Posts.GetFeedPagedAsync(
    currentUserId,
    skip: 0,              // ← HARDCODED! Never changes
    take: TrendingCandidateLimit  // ← HARDCODED! Always 500
);
```

### What Happens:
1. **Every request** calls `GetFeedPagedAsync(skip=0, take=500)`
2. **Every request** gets same 500 posts from DB (most recent)
3. **Pagination** only works within those 500 posts in memory
4. **Result**: Can't see beyond post #500

### Example Flow:
```
User Request: GET /api/posts/feed?page=1
  → Service: skip=0, take=500
  → DB returns: Posts [1000-501] (500 most recent)
  → Pagination: Show [1-20] ✅

User Request: GET /api/posts/feed?page=26
  → Service: skip=0, take=500  ← SAME!
  → DB returns: Posts [1000-501]  ← SAME!
  → Pagination: Try to show [501-520]
  → NOT IN MEMORY → Empty! ❌
```

---

## Ideal Architecture (What It Should Be)

### Code Change:
```csharp
// What it should be:
public async Task<PagedResult<PostResponse>> GetFeedAsync(
    Guid currentUserId, 
    int page, 
    int pageSize,
    int candidateOffset = 0)  // ← NEW parameter
{
    var (candidates, _) = await _uow.Posts.GetFeedPagedAsync(
        currentUserId,
        skip: candidateOffset,      // ← DYNAMIC!
        take: TrendingCandidateLimit
    );
    // ... rest is same
}
```

### What Happens:
1. **Page 1**: `candidateOffset=0` → Gets posts [1000-501]
2. **Page 26**: `candidateOffset=500` → Gets posts [500-1]
3. **Page 26+**: `candidateOffset=1000` → Gets next 500 posts
4. **Result**: Can scroll infinitely!

### Example Flow (Fixed):
```
User Request: GET /api/posts/feed?page=1&offset=0
  → Service: skip=0, take=500
  → DB returns: Posts [1000-501]
  → Pagination: Show [1-20] ✅

User Request: GET /api/posts/feed?page=26&offset=500
  → Service: skip=500, take=500  ← DIFFERENT!
  → DB returns: Posts [500-1]  ← NEXT BATCH!
  → Pagination: Show [501-520] ✅

User Request: GET /api/posts/feed?page=51&offset=1000
  → Service: skip=1000, take=500
  → DB returns: Posts [very old]
  → Pagination: Show [1001-1020] ✅
```

---

## Impact Matrix

| Scenario | Current | Ideal |
|----------|---------|-------|
| **1000 total posts in feed** | See only 500 | See all 1000 |
| **Page 1 (items 1-20)** | ✅ Works | ✅ Works |
| **Page 25 (items 481-500)** | ✅ Works | ✅ Works |
| **Page 26 (items 501-520)** | ❌ Empty | ✅ Works |
| **Page 50+** | ❌ All empty | ✅ All work |
| **Infinite scroll** | ❌ Breaks at ~500 | ✅ Works |

---

## Why This Limitation Exists

```
Decision Tree:

Need trending algorithm?
  Yes → Need to score posts
      → Need all posts in memory
      → Memory is limited
      → Cap at 500 "good enough"
      → Performance optimized
      → But loses pagination
      
vs.

Need infinite scroll?
  Yes → Need to paginate at DB level
      → Fetch 500 at a time
      → Score each batch separately
      → Slower per request
      → But works infinitely

They chose: Performance ✅ over Features ❌
```

---

## Quick Comparison

```
╔═══════════════════════════════════════════════════════════╗
║         CURRENT (Limited 500)   vs   IDEAL (Unlimited)   ║
╠═══════════════════════════════════════════════════════════╣
║                                                           ║
║  GetFeedPagedAsync(skip=0, take=500)                     ║
║         ↓                                                 ║
║  Returns [Post 1000-501]  (always same)                  ║
║         ↓                                                 ║
║  Paginate in-memory                                       ║
║  Page 1-25: ✅                                            ║
║  Page 26+: ❌                                             ║
║                                                           ║
║  ------- vs -------                                       ║
║                                                           ║
║  GetFeedPagedAsync(skip=offset, take=500)                ║
║         ↓                                                 ║
║  Returns different 500 per offset                        ║
║         ↓                                                 ║
║  Paginate that batch                                      ║
║  Page 1-25: ✅ (batch 1)                                  ║
║  Page 26-50: ✅ (batch 2)                                 ║
║  Page 51+: ✅ (batch 3+)                                  ║
║                                                           ║
╚═══════════════════════════════════════════════════════════╝
```

---

## Data Numbers (Your Case)

### Your Feed Composition:
```
Posts per person:      ~15 avg
Following count:       13 people
Your own posts:        5
Total:                 (13 × 15) + 5 = 200 posts

Max viewable:          500
Overhead:              300 posts wasted capacity

Conclusion: You CAN see all 200 posts! ✅
But if you follow more active people:
  50 people × 15 posts = 750 posts
  → Only see 500
  → Can't scroll past 25 pages ❌
```

---

## The Problem You Identified

> "Dữ liệu seed fresh nhưng vẫn hạn chế scroll?"

**Yes!** This is the issue:

```
✅ FIXED BY US:
   Posts are now 0-30 days old (fresh)
   No "data too old" problem anymore
   
❌ NOT FIXED (Architectural):
   Service only loads 500 posts per request
   Pagination hardcoded to in-memory
   Can't access beyond post #500
   
RESULT:
   Feed shows fresh posts ✅
   But limited to 500 posts max ❌
```

---

## 3 Ways to Fix (Priority Order)

### 🥇 **Priority 1: Quick Fix (5 min)**
```csharp
// Add offset parameter to GetFeedAsync
public async Task<PagedResult<PostResponse>> GetFeedAsync(
    Guid currentUserId, 
    int page, 
    int pageSize,
    int offset = 0)  // ← Add this
{
    var (candidates, _) = await _uow.Posts.GetFeedPagedAsync(
        currentUserId,
        skip: offset,  // ← Use it
        take: TrendingCandidateLimit
    );
    // ...
}

// API: /api/posts/feed?page=1&pageSize=20&offset=0
//      /api/posts/feed?page=26&pageSize=20&offset=500
```
**Cost**: 5 minutes  
**Benefit**: Infinite scroll works!

---

### 🥈 **Priority 2: Increase Limit (1 min)**
```csharp
private const int TrendingCandidateLimit = 2000; // Was 500

// More memory usage, slower scoring
// But can now see 2000 posts before hitting limit
```
**Cost**: 1 minute  
**Benefit**: Bumps limit from 500 → 2000

---

### 🥉 **Priority 3: Cache Scores (20 min)**
```csharp
// Pre-compute trending scores every 5 minutes
// Cache top 500 by score
// Pagination just reads cache

// Fastest performance
// But 5-minute staleness
```
**Cost**: 20 minutes  
**Benefit**: Best performance + infinite scroll

---

## Summary for Your Question

| Question | Answer |
|----------|--------|
| **"Data fresh on 4/10?"** | ✅ Yes, 0-30 days old |
| **"Can FE scroll all?"** | ❌ No, max 500 posts |
| **"Why limited?"** | Service hardcodes skip=0, take=500 |
| **"Limitation in FE?"** | ❌ No, limitation in Backend |
| **"How to fix?"** | Add offset parameter to service (5 min) |
| **"Practical impact?"** | Works for ~200 posts, breaks at ~500+ |

---

## Files to Understand This

1. **PostService.cs** (Line 76-79): The hardcoded skip=0
2. **PostRepository.cs** (Line 132-149): Repository that COULD handle pagination but doesn't
3. **FEED-SCROLLING-LIMITATIONS.md**: Full technical analysis
4. **FEED-SCROLLING-LIMIT-EXPLAINED.md**: Visual explanation

