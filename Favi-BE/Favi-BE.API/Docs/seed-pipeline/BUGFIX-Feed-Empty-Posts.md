# Bug Fix: Feed Not Showing Posts

## Problem Summary

Users' feeds were empty even though they had followers (10+) and were following others (13+). All seeded posts were being filtered out due to age validation.

## Root Cause

The seed pipeline was using **hardcoded anchor dates** that were static and became stale:

### Affected Files:
1. **SeedPostsStep.cs** (line 12)
   ```csharp
   private static readonly DateTime SeedAnchorUtc = new(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
   ```
   - Posts created: `2024-10-03` to `2025-01-01` (90 days before anchor)

2. **SeedFollowsStep.cs** (lines 146-153)
   - Follow edges created: Same 90-day window

3. **SeedEngagementStep.cs** (lines 13, 458)
   - Reactions, Comments, Reposts: Same 90-day window

### Feed Filter Logic

In `PostService.GetFeedAsync()` (line 92-93):
```csharp
var ageHours = (now - post.CreatedAt).TotalHours;
if (ageHours > MaxAgeHours) continue;  // MaxAgeHours = 720 hours = 30 days
```

**Timeline:**
- Seed anchor: `2025-01-01`
- Seed window: 90 days before anchor = `~2024-10-03` to `2025-01-01`
- Current date: Late January 2025 or later
- Age of posts: >90 days → **ALL FILTERED OUT** ❌

## Solution

Changed all seed date generation to use **current time dynamically** instead of hardcoded dates:

### Changes Made:

#### 1. SeedPostsStep.cs
**Before:**
```csharp
private static readonly DateTime SeedAnchorUtc = new(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

private static DateTime BuildCreatedAt(SeedContext seedContext)
{
    return SeedAnchorUtc
        .AddDays(-seedContext.Random.Next(0, 90))
        .AddHours(seedContext.Random.Next(0, 24))
        .AddMinutes(seedContext.Random.Next(0, 60));
}
```

**After:**
```csharp
private static DateTime BuildCreatedAt(SeedContext seedContext)
{
    var now = DateTime.UtcNow;
    return now
        .AddDays(-seedContext.Random.Next(0, 30))
        .AddHours(-seedContext.Random.Next(0, 24))
        .AddMinutes(-seedContext.Random.Next(0, 60));
}
```

#### 2. SeedFollowsStep.cs (lines 146-153)
Same pattern: Removed hardcoded `new DateTime(2025, 1, 1, ...)` and use `DateTime.UtcNow` with 30-day window.

#### 3. SeedEngagementStep.cs (lines 13, 456-462)
Same pattern: Removed hardcoded anchor and made timestamps relative to current time.

## Key Changes:
- ✅ Removed static `SeedAnchorUtc` constants
- ✅ Use `DateTime.UtcNow` for dynamic dates
- ✅ Changed range from 90 days to **30 days** (aligns with `MaxAgeHours = 720`)
- ✅ Fixed negative time additions (was `AddDays(-x)` with `+` operators, now consistent)

## Testing After Fix

1. **Re-seed the database** after this fix deploys
2. **Verify Feed:**
   - Login as any seeded user
   - Access `/api/posts/feed` endpoint
   - Should see posts from followers

3. **Expected Result:**
   - Feed should show posts created within the last 30 days
   - Posts should have valid privacy levels (80% Public)
   - Trending score calculation should work properly

## Additional Notes

This fix ensures that:
- ✅ Posts in feed are always within the valid age window
- ✅ Seed data is always fresh relative to current server time
- ✅ No manual date updates needed when re-seeding
- ✅ Aligned with privacy filtering logic (`CanViewPostAsync`)

## Migration Path

**For existing databases:**
- Option 1: Re-run seed pipeline (reset + re-seed)
- Option 2: Manual update posts' `CreatedAt` to recent dates:
  ```sql
  UPDATE Posts 
  SET CreatedAt = DATEADD(DAY, -RAND() * 30, GETUTCDATE())
  WHERE CreatedAt < DATEADD(DAY, -30, GETUTCDATE());
  ```
