# 📊 Logic Feed Posts - Chi tiết từ Seed đến Display

## 🎯 Tóm tắt

Feed posts không phải hiển thị random, mà theo **2 giai đoạn**:

### **Giai đoạn 1: SEED (SeedEngagement.cs)**
- Dùng **weighted random** để chọn posts đã tồn tại
- "Hot posts" được ưu tiên hơn (weight 7x so với posts bình thường)

### **Giai đoạn 2: DISPLAY (PostService.GetFeedAsync)**
- Chọn candidates từ repo (user's own posts + people they follow)
- **Filter & Score** theo Trending Algorithm
- **Sort** posts theo TrendingScore cao → thấp

---

## 📌 PHASE 1: SEED - Cách chọn Posts để tạo Engagement

### 1️⃣ Xác định "HOT Posts"
**File**: `SeedEngagement.cs` line 324-329

```csharp
private static HashSet<Guid> BuildHotPostSet(IReadOnlyList<Post> posts)
{
    var hotCount = Math.Max(1, (int)Math.Ceiling(posts.Count * 0.08));
    return posts
        .OrderBy(p => StableSeed.FromString($"hot:{p.Id}"))
        .Take(hotCount)
        .Select(p => p.Id)
        .ToHashSet();
}
```

**Logic:**
- `0.08` = 8% posts được coi là "hot"
- VD: 10,000 posts → 800 hot posts
- `StableSeed.FromString()` = deterministic hash (lặp lại cùng kết quả)
- Hot posts = posts có ID nhỏ nhất theo thứ tự hash

---

### 2️⃣ Chọn Post Weighted Random
**File**: `SeedEngagement.cs` line 338-355

```csharp
private static Post PickPostWeighted(IReadOnlyList<Post> posts, HashSet<Guid> hotPosts, SeedContext seedContext)
{
    var totalWeight = 0d;
    var weights = new double[posts.Count];

    for (var i = 0; i < posts.Count; i++)
    {
        // ⭐ Nếu là HOT post → weight = 7, else 1
        var weight = hotPosts.Contains(posts[i].Id) ? 7d : 1d;
        weights[i] = weight;
        totalWeight += weight;
    }

    var roll = seedContext.Random.NextDouble() * totalWeight;
    for (var i = 0; i < posts.Count; i++)
    {
        roll -= weights[i];
        if (roll <= 0)
            return posts[i];
    }

    return posts[^1];
}
```

**Ví dụ cụ thể:**

Giả sử có 3 posts:
- Post A (hot): weight = 7
- Post B (normal): weight = 1  
- Post C (normal): weight = 1
- **Total weight = 9**

```
Xác suất chọn:
- Post A: 7/9 = 77.8% ⭐
- Post B: 1/9 = 11.1%
- Post C: 1/9 = 11.1%
```

**Cơ chế "weighted random":**
```
Roll random 0-9:
  [0-7)     → Post A ✅ (7 in 9)
  [7-8)     → Post B
  [8-9)     → Post C
```

---

## 🎬 PHASE 2: DISPLAY - Cách chọn Posts để hiển thị trên Feed

### 📋 Flow Chi tiết

```
User request: GET /api/posts/feed?page=1&pageSize=20
    ↓
1. GetFeedPagedAsync(userId, skip=0, take=500)
   - Query posts từ: (user's own posts) OR (posts từ people user follow)
   - Condition: NOT deleted AND NOT archived
   - Order by CreatedAt DESC
   - Result: 500 candidates
    ↓
2. Privacy & Age Filter
   - Loop 500 candidates:
     • CanViewPostAsync(post, userId)?
       - Is Public? YES → Pass ✅
       - Is Followers? Check: userId follow post.author? 
       - Is Private? NO → Skip ❌
     • Age check: (now - CreatedAt) < 720 hours (30 days)? YES → Pass ✅
    ↓
3. Trending Score Calculation (Chỉ cho posts pass filter)
   - Score = Engagement × Decay × Velocity
   - Engagement = 1.0 + (Likes × 1.0) + (Comments × 3.0) + (Shares × 5.0) + (Views × 0.2)
   - Decay = e^(-0.1 × age_hours)  [exponential decay]
   - Velocity = (new interactions in last 1 hour) / 1 hour
   - Result: 50-200 valid posts with scores
    ↓
4. Sort & Paginate
   - Sort by Score DESC
   - Skip (page-1) × pageSize
   - Take pageSize (20)
    ↓
5. Map to Response
   - Convert Post entities to PostResponse
   - Return PagedResult
```

---

## 📐 Trending Score Formula

**File**: `PostService.cs` line 883-922

```csharp
Score(post, now) = Engagement × Decay × (1 + Beta × Velocity)
```

### **Component 1: Engagement (Base + Interactions)**
```csharp
Engagement = 1.0 + W_Like × L + W_Comment × C + W_Share × S + W_View × V

Where:
  W_Like    = 1.0   (mỗi like)
  W_Comment = 3.0   (mỗi comment)
  W_Share   = 5.0   (mỗi share)
  W_View    = 0.2   (mỗi view)
  L, C, S, V = counts
```

**Ví dụ:**
- Post mới, 0 interactions: Engagement = 1.0
- 5 likes, 2 comments: Engagement = 1.0 + 5×1.0 + 2×3.0 = 12.0

### **Component 2: Time Decay**
```csharp
Decay = e^(-λ × age_hours)  
Where λ = 0.1, age_hours = hours since post created
```

**Timeline decay:**
```
Age:     0h        24h        48h        72h
Decay:   1.0       0.741      0.549      0.407
Score:   100%      74.1%      54.9%      40.7%
```

Older posts lose relevance exponentially.

### **Component 3: Velocity (Recent Activity Boost)**
```csharp
Velocity = (recent interactions in last 1 hour) / 1 hour
Score boost = 1 + Beta × Velocity  (Beta = 0.5)
```

**Effect:**
- No recent activity: boost = 1.0x (no boost)
- 5 interactions/hour: boost = 1.0 + 0.5 × 5 = 3.5x 🚀

---

## 📊 Comparison: Seed Selection vs Feed Display

| Aspect | Seed Selection (PickPostWeighted) | Feed Display (ComputeTrendingScore) |
|--------|---|---|
| **Purpose** | Create engagement data | Rank posts for user |
| **Algorithm** | Weighted random | Deterministic scoring |
| **Hot posts boost** | 7× weight | Variable (depends on interactions) |
| **Time factor** | Ignored | Exponential decay |
| **Recency** | Any date (now fixed) | < 30 days only |
| **Privacy** | Ignored | Strictly checked |
| **Result** | Reactions, Comments, Reposts | Sorted feed |

---

## 🎯 Concrete Example: User's Feed Generation

**Scenario:** User has 13 followees

**Step 1: Get Candidates (500 posts max)**
```sql
SELECT TOP 500 *
FROM Posts p
WHERE (
  p.ProfileId = @userId 
  OR EXISTS (SELECT 1 FROM Follows f 
             WHERE f.FollowerId = @userId 
             AND f.FolloweeId = p.ProfileId)
)
AND p.DeletedDayExpiredAt IS NULL
AND p.IsArchived = 0
ORDER BY p.CreatedAt DESC
```
Result: 50-150 posts ✅

**Step 2: Filter by Privacy**
- Post A (ProfileId=user1): Privacy=Public → Pass ✅
- Post B (ProfileId=user2): Privacy=Followers, user follow user2 → Pass ✅
- Post C (ProfileId=user3): Privacy=Private, user ≠ author → Skip ❌
Result: 40-100 posts

**Step 3: Filter by Age**
- Post A (Created: 2 hours ago): 2 < 720 → Pass ✅
- Post B (Created: 50 days ago): 1200 > 720 → Skip ❌
Result: 30-80 posts

**Step 4: Compute Score for Each**
```
Post A:
  Likes: 15, Comments: 5
  Engagement = 1.0 + 15×1.0 + 5×3.0 = 31.0
  Age: 2 hours → Decay ≈ 0.98
  Recent: 3 interactions/hour → Velocity = 3.0, Boost = 1 + 0.5×3 = 2.5
  Score = 31.0 × 0.98 × 2.5 = 75.95 ⭐⭐⭐

Post B:
  Likes: 2, Comments: 0
  Engagement = 1.0 + 2×1.0 = 3.0
  Age: 20 hours → Decay ≈ 0.136
  Recent: 0 interactions → Velocity = 0, Boost = 1.0
  Score = 3.0 × 0.136 × 1.0 = 0.408
```

**Step 5: Sort & Paginate**
```
Rankings:
1. Post A (75.95) ⭐⭐⭐⭐⭐
2. Post D (45.20)
3. Post E (32.10)
...
20. Post Z (1.05)
```

**Result for User:** Shows Post A first (high engagement + recent)

---

## 🔍 Key Insights

### ✅ Why Hot Posts in Seed?
- Creates realistic engagement distribution
- 8% of posts get 77.8% of engagements
- Mirrors real social networks (Pareto 80/20)

### ✅ Why Trending Score?
- Balances **quality** (engagement) + **freshness** (decay) + **momentum** (velocity)
- Prevents old posts from dominating forever
- Boosts posts getting current interaction

### ✅ Why Privacy Check?
- Posts > 30 days are pruned
- Followers-only posts need verification
- Private posts only visible to owner

---

## 📝 Summary Table

```
┌─────────────────────────────────────────────────────────┐
│ SEED PHASE (Creating Test Data)                         │
├─────────────────────────────────────────────────────────┤
│ 1. BuildHotPostSet → 8% of posts marked as "hot"       │
│ 2. PickPostWeighted → Random selection (7× for hot)    │
│ 3. Result: Reactions, Comments, Reposts distributed    │
└─────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────┐
│ DISPLAY PHASE (Showing Posts to User)                   │
├─────────────────────────────────────────────────────────┤
│ 1. Query 500 candidates (user's + followees' posts)    │
│ 2. Privacy Filter (Public/Followers/Private check)      │
│ 3. Age Filter (< 30 days only)                          │
│ 4. Score Calculate (Engagement × Decay × Velocity)     │
│ 5. Sort DESC + Paginate                                 │
│ 6. Result: Feed ranked by TrendingScore                │
└─────────────────────────────────────────────────────────┘
```
