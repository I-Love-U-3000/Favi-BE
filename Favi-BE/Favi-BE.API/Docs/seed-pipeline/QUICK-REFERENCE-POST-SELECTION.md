# 🚀 Quick Reference: Post Selection Logic

## 2-Line Summary

**SEED**: "Hot posts" (8%) được chọn **7x tần suất** hơn posts bình thường → tạo realistic engagement distribution

**DISPLAY**: Posts được rank bằng **TrendingScore** = Engagement × Decay × Velocity (cân bằng quality + freshness + momentum)

---

## Key Numbers

### SEED Phase (SeedEngagement.cs)

| Metric | Value | Meaning |
|--------|-------|---------|
| Hot posts % | 8% | Của 10,000 posts → 800 hot |
| Hot weight | 7 | Tỉ lệ 7:1 so với normal |
| Normal weight | 1 | Baseline |
| **Practical ratio** | **37.8%** | Của 80K interactions → hot posts nhận |

### DISPLAY Phase (PostService.GetFeedAsync)

| Metric | Value | Effect |
|--------|-------|--------|
| Candidate limit | 500 | Max posts to score |
| Age limit | 720 hours | 30 days max |
| Like weight (W_Like) | 1.0 | 1 point/like |
| Comment weight (W_Comment) | 3.0 | 3 points/comment |
| Share weight (W_Share) | 5.0 | 5 points/share |
| View weight (W_View) | 0.2 | 0.2 points/view |
| Decay rate (λ) | 0.1 | Exponential decay |
| Velocity boost (β) | 0.5 | 1-hour momentum window |

---

## Simple Formula

```
🎯 TRENDING SCORE = Engagement × Decay × (1 + 0.5 × Velocity)

Engagement = 1.0 + L + 3C + 5S + 0.2V   (L=likes, C=comments, S=shares, V=views)
Decay = e^(-0.1 × age_hours)             (older = weaker)
Velocity = new_interactions_per_hour      (momentum = boost)
```

---

## Decision Tree: Will Post Show in My Feed?

```
                    GET /api/posts/feed
                           ↓
                  Fetch 500 candidates
                  (my posts + followees)
                           ↓
                  Privacy Check
                   /              \
              Public?          Follower Only?
               /YES \            /YES \
              ✅    NO → Private? ✅    NO
                        /YES \        (author only)
                       ✅     ❌       ❌
                       
                All pass? → Age Check < 30 days?
                              /YES \
                            ✅     ❌ (SKIP)
                            
                Pass all? → Compute Score
                           Score = ...formula...
                           
                        Sort Descending
                        
                      Paginate (page 1, size 20)
                      
                         Return Feed
```

---

## Real Example Scores

```
Recent Post (2h old, 15 likes, 5 comments, 3 recent interactions):
  Engagement = 1 + 15 + 15 = 31
  Decay = e^(-0.2) = 0.82
  Velocity = 1 + 0.5×3 = 2.5
  Score = 31 × 0.82 × 2.5 = 63.55 ⭐⭐⭐⭐⭐
  
Old Post (20h old, 3 likes, 1 comment, 0 recent):
  Engagement = 1 + 3 + 3 = 7
  Decay = e^(-2) = 0.14
  Velocity = 1 + 0 = 1.0
  Score = 7 × 0.14 × 1.0 = 0.98 👎
  
→ Recent post shows first!
```

---

## Code Locations

| What | File | Line |
|------|------|------|
| Hot post set | SeedEngagement.cs | 324 |
| Weighted random | SeedEngagement.cs | 338 |
| Query candidates | PostRepository.cs | 132 |
| Privacy check | PrivacyGuard.cs | 42 |
| Score formula | PostService.cs | 883 |
| Feed endpoint | PostController.cs | 85 |

---

## One More Thing: Why Weight 7:1?

```
Total posts: 10,000
Hot posts: 800 (8%)

If uniform random: each post picked = 1/10,000 = 0.01%
If weighted random: 
  Hot post = 7 / 14,800 = 0.047% (4.7× more!)
  Normal = 1 / 14,800 = 0.0068% (0.68x)

After 80K interactions:
  Hot posts: ~3,760 interactions (4.7%)
  Normal: ~76,240 interactions (95.3%)

← Mirrors real social media! (Power law distribution)
```

---

## TL;DR

| Phase | Algorithm | Output |
|-------|-----------|--------|
| **SEED** | Weighted Random (hot: 7x) | Reactions, Comments, Reposts |
| **DISPLAY** | Trending Score (E×D×V) | Ranked Feed |

Post appears in feed if:
- ✅ User has permission (public/follower/owner)
- ✅ Not too old (< 30 days)
- ✅ Has trending score
- ✅ Ranked in top 20 for pagination

🎯 **Result**: Fresh, engaging, relevant posts!
