# 🔄 Visual Flow: Post Selection - Seed to Feed

## 📊 Architecture Diagram

```
┌──────────────────────────────────────────────────────────────┐
│                    DATABASE (Posts)                          │
│  10,000 Posts: 800 hot (weight 7) + 9,200 normal (weight 1)│
└──────────────────────────────────────────────────────────────┘
                            ↓↓↓
        ┌───────────────────┴───────────────────┐
        │                                       │
   ╔════════════════╗              ╔═══════════════════╗
   ║  SEED PHASE    ║              ║ DISPLAY PHASE     ║
   ║  (Write)       ║              ║ (Read/Rank)       ║
   ╚════════════════╝              ╚═══════════════════╝
        ↓↓↓                              ↓↓↓
   ┌─────────────┐              ┌─────────────────┐
   │ Pick Posts  │              │ GetFeedPagedAsync
   │ Weighted    │              │ 500 candidates
   │ Random      │              └─────────────────┘
   │             │                     ↓
   │ 7:1 ratio   │              ┌─────────────────┐
   │ hot:normal  │              │ Privacy Filter
   └─────────────┘              │ • Public ✅
        ↓↓↓                      │ • Followers 🔒
   Generate:                     │ • Private ❌
   • Reactions                   └─────────────────┘
   • Comments                          ↓
   • Reposts                    ┌─────────────────┐
        ↓↓↓                      │ Age Filter
   Save to DB                    │ < 30 days ✅
```

---

## 🎯 Detailed Flow: How Posts Get Selected

### **SEED: Creating Test Data (One-time)**

```
Input: 10,000 Posts
  │
  ├─ Step 1: BuildHotPostSet
  │  ├─ Calculate: 10,000 × 0.08 = 800 hot posts
  │  ├─ Method: StableSeed.FromString($"hot:{PostId}")
  │  └─ Result: Consistent 800 hot posts
  │
  ├─ Step 2: PickPostWeighted (Loop to create engagement)
  │  ├─ For each new reaction/comment/repost:
  │  │  ├─ Generate random [0-9]:
  │  │  │  ├─ [0-7) → Hot post (77.8% chance)
  │  │  │  └─ [7-9) → Normal post (22.2% chance)
  │  │  └─ Create interaction on selected post
  │  │
  │  └─ Create 80,000-120,000 reactions
  │     Create 15,000-30,000 comments
  │     Create 1,000-2,000 reposts
  │
  └─ Result: Realistic engagement distribution
```

**Weight Calculation:**
```csharp
weights[i] = post in hotPosts ? 7.0 : 1.0;
totalWeight = sum of all weights
              = (800 × 7) + (9,200 × 1)
              = 5,600 + 9,200
              = 14,800

probability(pick hot post) = 5,600 / 14,800 = 37.84%  ← Practical ratio
                             (NOT 77.8% because many interactions spread)
```

---

### **DISPLAY: Ranking for User (Per Request)**

```
User Request: GET /api/posts/feed?page=1&pageSize=20
  │
  ├─ Step 1: GetFeedPagedAsync (Query)
  │  ├─ SELECT TOP 500 FROM Posts WHERE:
  │  │  ├─ (ProfileId = @userId
  │  │  │   OR EXISTS Follows WHERE FollowerId=@userId 
  │  │  │              AND FolloweeId=PostProfileId)
  │  │  ├─ AND DeletedDayExpiredAt IS NULL
  │  │  └─ AND IsArchived = 0
  │  ├─ ORDER BY CreatedAt DESC
  │  └─ Result: ~100-300 candidates
  │
  ├─ Step 2: Privacy Filter (In-Memory)
  │  ├─ For each candidate post:
  │  │  ├─ if post.Privacy == Public → PASS ✅
  │  │  ├─ if post.Privacy == Followers
  │  │  │  └─ Check: CanFollowAsync(viewerId, postAuthorId)?
  │  │  │      ├─ YES → PASS ✅
  │  │  │      └─ NO → SKIP ❌
  │  │  └─ if post.Privacy == Private
  │  │     └─ Only author → PASS ✅
  │  │        Others → SKIP ❌
  │  └─ Result: ~80-250 valid posts
  │
  ├─ Step 3: Age Filter (In-Memory)
  │  ├─ For each valid post:
  │  │  ├─ ageHours = (now - post.CreatedAt).TotalHours
  │  │  ├─ if ageHours < 720 (30 days) → PASS ✅
  │  │  └─ if ageHours >= 720 → SKIP ❌ (too old)
  │  └─ Result: ~50-180 fresh valid posts
  │
  ├─ Step 4: Compute TrendingScore for Each
  │  ├─ For each post:
  │  │  ├─ A. Calculate Engagement
  │  │  │    Engagement = 1.0 + 
  │  │  │                 (Likes × 1.0) +
  │  │  │                 (Comments × 3.0) +
  │  │  │                 (Shares × 5.0) +
  │  │  │                 (Views × 0.2)
  │  │  │
  │  │  ├─ B. Calculate Decay (age penalty)
  │  │  │    Decay = e^(-0.1 × ageHours)
  │  │  │    e.g., 24h old → e^(-2.4) ≈ 0.091
  │  │  │
  │  │  ├─ C. Calculate Velocity (momentum)
  │  │  │    RecentInteractions = interactions in last 1 hour
  │  │  │    Velocity = RecentInteractions / 1.0 (per hour)
  │  │  │    VelocityBoost = 1 + (0.5 × Velocity)
  │  │  │    e.g., 3 new likes in last hour → 1 + 0.5×3 = 2.5x
  │  │  │
  │  │  └─ D. Final Score
  │  │     Score = Engagement × Decay × VelocityBoost
  │  │
  │  └─ Result: Each post has numeric Score
  │
  ├─ Step 5: Sort by Score (Descending)
  │  ├─ Order by Score DESC
  │  └─ Result: Posts ranked by relevance
  │
  ├─ Step 6: Paginate
  │  ├─ Skip (page-1) × pageSize = (1-1) × 20 = 0
  │  ├─ Take 20 items
  │  └─ Result: First 20 posts for feed
  │
  └─ Step 7: Map to Response
     └─ Return PostResponse[] with metadata

Output: { items: 20 posts, total: 145, page: 1, pageSize: 20 }
```

---

## 📈 Score Calculation Example

### **Post A (2 hours old, got 5 new comments)**
```
Interactions:    15 likes, 5 comments, 0 shares
Engagement = 1.0 + (15 × 1.0) + (5 × 3.0) + (0 × 5.0) + (0 × 0.2)
           = 1.0 + 15 + 15 + 0 + 0
           = 31.0

Age:        2 hours
Decay = e^(-0.1 × 2) = e^(-0.2) ≈ 0.819

Recent:     5 interactions in last 1 hour
Velocity = 5.0 interactions/hour
Boost = 1 + (0.5 × 5.0) = 3.5x

FINAL SCORE = 31.0 × 0.819 × 3.5
            = 88.47  ⭐⭐⭐⭐⭐ TOP RANK!
```

### **Post B (20 hours old, no recent activity)**
```
Interactions:    3 likes, 1 comment, 0 shares
Engagement = 1.0 + (3 × 1.0) + (1 × 3.0) 
           = 7.0

Age:        20 hours
Decay = e^(-0.1 × 20) = e^(-2.0) ≈ 0.135

Recent:     0 interactions in last 1 hour
Velocity = 0
Boost = 1 + 0 = 1.0

FINAL SCORE = 7.0 × 0.135 × 1.0
            = 0.945  (very low, buried in feed)
```

---

## 🔄 Complete Example Flow

```
USER PROFILE: user_123
FOLLOWING: 13 people
REQUEST: GET /api/posts/feed?page=1&pageSize=20

┌─────────────────────────────────────────────────┐
│ QUERY: 500 most recent posts from             │
│ - user_123's own posts (5)                     │
│ - 13 followees' posts (150 posts)              │
│ - Total: 150 candidates                        │
└─────────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────────┐
│ FILTER 1: Privacy Check                         │
│ ✅ Public posts: 120                            │
│ ✅ Followers posts (user follows): 18           │
│ ❌ Private posts (skip): 12                     │
│ → Remaining: 138 posts                          │
└─────────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────────┐
│ FILTER 2: Age Check (< 30 days)                │
│ ✅ < 30 days: 120 posts                         │
│ ❌ > 30 days: 18 posts (removed)               │
│ → Remaining: 120 posts                          │
└─────────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────────┐
│ SCORE CALCULATION (for 120 posts)              │
│ Post 1: Score 87.54  ⭐ RANK #1                │
│ Post 2: Score 65.20  ⭐ RANK #2                │
│ Post 3: Score 54.30  ⭐ RANK #3                │
│ ...                                             │
│ Post 20: Score 12.40 ⭐ RANK #20               │
│ Post 21: Score 11.90                           │
│ Post 22: Score 10.20                           │
│ ...                                             │
└─────────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────────┐
│ PAGINATE (page=1, pageSize=20)                 │
│ Take posts 1-20 from ranking                   │
└─────────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────────┐
│ RESPONSE                                        │
│ {                                               │
│   "items": [ Post, Post, ... ],  // 20 posts  │
│   "page": 1,                                   │
│   "pageSize": 20,                              │
│   "totalCount": 120                            │
│ }                                               │
└─────────────────────────────────────────────────┘
```

---

## 🎯 Key Formula Summary

| Formula | Purpose | Example |
|---------|---------|---------|
| `Weight = hot ? 7 : 1` | Seed: bias toward hot posts | 800 hot posts picked 3.8x more often |
| `Decay = e^(-λ×age)` | Penalize old posts | 24h old = 91% reduction |
| `Velocity = ΔE/Δt` | Reward momentum | 5 interactions/hour = 2.5x boost |
| `Score = E × D × V` | Final ranking | Combines all factors |

---

## 💡 Why This Design?

✅ **Hot posts in seed** = Realistic engagement (Pareto 80/20)
✅ **Trending score** = Balances quality + freshness + momentum
✅ **Privacy filter** = Prevents unauthorized viewing
✅ **Age limit** = Keeps feed fresh, not stale
✅ **Weighted random** = Deterministic but varied distribution
