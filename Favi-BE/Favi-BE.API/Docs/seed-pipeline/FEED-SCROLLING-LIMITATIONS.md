# 🚨 Feed Scrolling Limitations - Service Layer Analysis

## The Problem You Found

Yes, you're right! After the seed fix, posts are all fresh (0-30 days old). But **can user scroll through ALL of them?** 

**Answer: NO** ❌ There are architectural limitations in the service layer.

---

## 🔴 Critical Limitation: TrendingCandidateLimit = 500

**File**: `PostService.cs` line 35

```csharp
private const int TrendingCandidateLimit = 500; // tối đa ứng viên để tính trending
```

**Current Flow**:
```csharp
var (candidates, _) = await _uow.Posts.GetFeedPagedAsync(
    currentUserId,
    skip: 0,              // ⚠️ ALWAYS ZERO!
    take: TrendingCandidateLimit  // ⚠️ ALWAYS 500!
);
```

---

## 📊 Real Scenario

### **Setup:**
- User follows: 13 people
- Those 13 people have: ~150 recent posts (< 30 days)
- You have: 5 own posts
- **Total in feed**: ~155 posts ✅

### **What User Can See:**
- Page 1 (items 1-20): Top 20 by trending score ✅
- Page 2 (items 21-40): Next 20 by trending score ✅
- ...
- Page 8 (items 141-160): Can show remaining 15 posts ✅
- **Page 9+**: EMPTY! No posts! ❌

```
PostService Loop Flow:
┌──────────────────────────────────┐
│ GetFeedAsync(userId, page=9)     │
├──────────────────────────────────┤
│ 1. GetFeedPagedAsync(            │
│      skip=0,                     │ ← Always 0!
│      take=500)                   │ ← Always 500!
│    Result: Same 500 candidates!  │
│                                  │
│ 2. Filter by privacy → 155       │
│ 3. Score all 155                 │
│ 4. Skip (9-1)×20 = 160           │ ← Beyond 155!
│ 5. Take 20                        │
│                                  │
│ Result: [] (empty)               │
└──────────────────────────────────┘
```

---

## ❌ Why This Happens

The code **hardcodes `skip: 0`**:

```csharp
// Line 76-80: PostService.cs
var (candidates, _) = await _uow.Posts.GetFeedPagedAsync(
    currentUserId,
    skip: 0,              // ← This should be parameterized!
    take: TrendingCandidateLimit
);

// Then later: Pagination is done AFTER scoring
var skip = (page - 1) * pageSize;  // page=9, pageSize=20 → skip=160
var pageItems = ordered
    .Skip(skip)  // Skip 160 items in the 155-item list!
    .Take(pageSize)
    .ToList();
```

**Problem**:
1. Repository always gets same 500 posts (skip: 0)
2. Pagination happens in-memory on 500 posts
3. If fewer than 500 posts pass filter, you hit empty pages fast

---

## 📈 The "Candidate Pool" Architecture Issue

```
┌─────────────────────────────────────────────────────────┐
│ GetFeedPagedAsync(userId, skip=0, take=500)            │
├─────────────────────────────────────────────────────────┤
│ SELECT * FROM Posts WHERE                              │
│   (ProfileId = userId OR user follows author)          │
│   AND NOT deleted AND NOT archived                      │
│ ORDER BY CreatedAt DESC                                │
│ OFFSET skip (0)    ← Always from top!                  │
│ LIMIT take (500)   ← Max 500 posts                     │
└─────────────────────────────────────────────────────────┘
          ↓ (Always same 500)
      Feed Algorithm:
      1. Privacy filter → 155 pass
      2. Age filter → 140 pass
      3. Trending score → all 140 scored
      4. Sort desc by score
      5. Paginate in-memory
                ↓
      Page 1: [1-20] ✅
      Page 2: [21-40] ✅
      ...
      Page 7: [121-140] ✅
      Page 8: [141-160] ❌ Empty (only 140 exist)
```

---

## 🎯 Specific Limitations

### **1. Limited Candidate Pool (500 max)**
```
Scenario: User follows 100 people, total 2,000 posts

Backend only looks at top 500 most recent:
- Posts 1-500: Might be considered ✅
- Posts 501-2000: NEVER seen ❌

Even if you have 2,000 posts to scroll through,
you can only see the top 500!
```

### **2. Pagination is In-Memory Only**
```
GetFeedAsync() Logic:
Step 1: Fetch 500 posts from DB
Step 2: Filter privacy, age, compute score (in C#)
Step 3: Paginate in-memory (.Skip().Take())

If 500 posts become 50 after filtering:
→ Can only view page 1-3 (60 items max)
→ Pages 4+ are empty
```

### **3. No Skip Parameter in Feed Request**
```
Current API:
GET /api/posts/feed?page=1&pageSize=20

What it should be (for true scrolling):
GET /api/posts/feed?page=1&pageSize=20&offset=0

Then service could do:
GetFeedPagedAsync(userId, skip=offset, take=offset+500)
```

### **4. Hardcoded `skip: 0`**
```csharp
// Current (broken for scrolling):
var (candidates, _) = await _uow.Posts.GetFeedPagedAsync(
    currentUserId,
    skip: 0,        // ← Always 0!
    take: 500
);

// Should be (for true pagination):
var (candidates, _) = await _uow.Posts.GetFeedPagedAsync(
    currentUserId,
    skip: (page - 1) * 500,  // ← Parameterized!
    take: 500
);
```

---

## 🔴 Concrete Example: Why It Breaks

**Setup**: User has 400 total posts in feed (from self + followees)

```
RequestFlow:

Page 1: GET /api/posts/feed?page=1&pageSize=20
├─ Fetch 500 candidates (skip=0) → 400 posts
├─ Filter & score → 380 valid
├─ Paginate: skip=0, take=20
└─ Result: ✅ Posts [1-20]

Page 2: GET /api/posts/feed?page=2&pageSize=20
├─ Fetch 500 candidates (skip=0) → SAME 400 posts!
├─ Filter & score → SAME 380 valid
├─ Paginate: skip=20, take=20
└─ Result: ✅ Posts [21-40]

...

Page 19: GET /api/posts/feed?page=19&pageSize=20
├─ Fetch 500 candidates (skip=0) → SAME 400 posts!
├─ Filter & score → SAME 380 valid
├─ Paginate: skip=360, take=20
└─ Result: ✅ Posts [361-380]

Page 20: GET /api/posts/feed?page=20&pageSize=20
├─ Fetch 500 candidates (skip=0) → SAME 400 posts!
├─ Filter & score → SAME 380 valid
├─ Paginate: skip=380, take=20
├─ Only 0 items available (380 total)
└─ Result: ❌ Empty [] 

User can't scroll past page 19!
```

---

## 📋 Limitations Summary Table

| Aspect | Current | Should Be | Impact |
|--------|---------|-----------|--------|
| **Candidate fetch** | `skip=0, take=500` | `skip=page×500, take=500` | Can't see beyond 500 posts |
| **Pagination logic** | In-memory only | Database + in-memory | Wasteful, limited |
| **API parameters** | `page, pageSize` | `page, pageSize, offset` | Can't skip to later posts |
| **Repository call** | Always same skip | Dynamic skip | Always gets same posts |
| **Max viewable posts** | 500 posts | Unlimited | Only 500 in pool |

---

## 💡 Why Was It Designed This Way?

**Theory**: Trending algorithm complexity

```
Trending Score needs:
- Count of likes/comments (not just existence) ✅
- Decay by age ✅
- Velocity (recent interactions) ✅

To compute this for 10,000 posts in memory = slow
So they capped at 500 = "good enough trending pool"
(probably for performance on slow systems)
```

---

## 🛠️ How to Fix It (Architecture Change)

### **Option 1: Dynamic Candidate Pool**

```csharp
public async Task<PagedResult<PostResponse>> GetFeedAsync(
    Guid currentUserId, 
    int page, 
    int pageSize,
    int offset = 0)  // ← New parameter
{
    var candidateSkip = offset;
    var (candidates, total) = await _uow.Posts.GetFeedPagedAsync(
        currentUserId,
        skip: candidateSkip,  // ← Dynamic!
        take: TrendingCandidateLimit
    );
    
    // Rest is same...
    var skip = (page - 1) * pageSize;
    // ...
}
```

### **Option 2: Increase Candidate Limit (Quick Fix)**

```csharp
private const int TrendingCandidateLimit = 2000; // ← From 500 to 2000

// Trade-off: More memory, slower scoring, but can see 2000 posts
```

### **Option 3: Cache Trending Scores**

```
Problem: Computing trending scores for 500 posts per request is slow
Solution: Pre-compute trending scores every 5 minutes:
  • Cache top 500 by score
  • Pagination just reads from cache
  • Faster, more scrollable

Cost: 5-min staleness in trending (acceptable)
```

---

## 📊 Current vs Ideal Behavior

```
CURRENT (Limited):
┌─────────────────────┐
│ User requests page 1│
└────────────┬────────┘
             ↓
    Fetch 500 most recent
             ↓
    Filter & score
             ↓
    Show items 1-20 ✅
             ↓
┌─────────────────────┐
│ User requests page 8│
└────────────┬────────┘
             ↓
    Fetch SAME 500 recent (skip=0)
             ↓
    Filter & score
             ↓
    Try to show items 141-160
    Only 140 exist → ❌ Empty!


IDEAL (Unlimited):
┌─────────────────────────────┐
│ User requests page 1 (page- │
│ Size=20, offset=0)          │
└────────────┬────────────────┘
             ↓
    Fetch 500 posts from DB (skip=0, take=500)
             ↓
    Filter & score all 500
             ↓
    Show items 1-20 ✅
             ↓
┌─────────────────────────────┐
│ User requests page 8 (page= │
│ Size=20, offset=1000)       │
└────────────┬────────────────┘
             ↓
    Fetch NEXT 500 posts from DB (skip=1000, take=500)
             ↓
    Filter & score those 500
             ↓
    Show items 141-160 ✅
             ↓
    Can scroll forever!
```

---

## 🎯 Bottom Line

**After seed fix (posts are 0-30 days):**
- ✅ Feed shows relevant recent posts
- ✅ No "too old" filtering issue
- ❌ **But can't scroll past 500 posts in the pool**
- ❌ **Can't access older followees' posts (beyond 500)**

**Practical Impact:**
- Most users see 20-50 posts per scroll session = **OK** ✅
- Power users who scroll endlessly = **Will hit empty pages** ❌

**Performance Impact:**
- Limiting to 500 = faster trending calculation ✅
- But sacrifices infinite scroll feature ❌

---

## 🔧 Recommended Fix (Priority)

```csharp
// In GetFeedAsync, add offset parameter:

public async Task<PagedResult<PostResponse>> GetFeedAsync(
    Guid currentUserId, 
    int page = 1, 
    int pageSize = 20,
    int candidateOffset = 0)  // ← New
{
    var skip = (page - 1) * pageSize;

    var (candidates, total) = await _uow.Posts.GetFeedPagedAsync(
        currentUserId,
        skip: candidateOffset,      // ← Use it
        take: TrendingCandidateLimit
    );

    // ... rest of logic ...
    
    // Update API endpoint:
    // GET /api/posts/feed?page=1&pageSize=20&offset=0
}
```

Cost: 5 minutes of work, enables infinite scroll! 🚀
