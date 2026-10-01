using Favi_BE.Data;
using Favi_BE.Interfaces.Services;
using Favi_BE.Modules.Auth.Application.Contracts;
using Favi_BE.Modules.Auth.Application.Contracts.ReadModels;
using Favi_BE.Modules.Auth.Application.Responses;
using Microsoft.EntityFrameworkCore;

namespace Favi_BE.API.Application.Auth;

/// <summary>
/// Implements IAuthQueryReader (Auth module port) using AppDbContext AsNoTracking.
/// No mutations allowed — pure read side.
/// </summary>
internal sealed class AuthQueryReaderAdapter : IAuthQueryReader
{
    private readonly AppDbContext _db;
    private readonly IPrivacyGuard _privacy;

    public AuthQueryReaderAdapter(AppDbContext db, IPrivacyGuard privacy)
    {
        _db = db;
        _privacy = privacy;
    }

    public async Task<CurrentUserDto?> GetCurrentUserAsync(Guid profileId, CancellationToken ct = default)
    {
        var profile = await _db.Profiles
            .AsNoTracking()
            .Where(p => p.Id == profileId)
            .Select(p => new CurrentUserDto(
                p.Id,
                p.Username,
                p.DisplayName,
                p.AvatarUrl,
                p.Role.ToString().ToLower()))
            .FirstOrDefaultAsync(ct);

        return profile;
    }

    public Task<bool> ProfileExistsAsync(Guid profileId, CancellationToken ct = default)
        => _db.Profiles.AsNoTracking().AnyAsync(p => p.Id == profileId, ct);

    public async Task<ProfileReadModel?> GetProfileByIdAsync(Guid profileId, Guid? viewerId, CancellationToken ct = default)
    {
        var profile = await _db.Profiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == profileId, ct);

        if (profile is null) return null;
        if (!await _privacy.CanViewProfileAsync(profile, viewerId)) return null;

        var email = await _db.EmailAccounts
            .AsNoTracking()
            .Where(e => e.Id == profileId)
            .Select(e => e.Email)
            .FirstOrDefaultAsync(ct);

        var followersCount = await _db.Follows.AsNoTracking().CountAsync(f => f.FolloweeId == profileId, ct);
        var followingCount = await _db.Follows.AsNoTracking().CountAsync(f => f.FollowerId == profileId, ct);

        return MapProfile(profile, email, followersCount, followingCount);
    }

    public async Task<IReadOnlyList<ProfileReadModel>> GetRecommendedProfilesAsync(
        Guid viewerId, int skip, int take, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        // 1. Identify viewer's followings
        var myFollowingIds = await _db.Follows
            .AsNoTracking()
            .Where(f => f.FollowerId == viewerId)
            .Select(f => f.FolloweeId)
            .ToHashSetAsync(ct);

        // 2. Graph candidates (Mutual Follows / 2nd Degree connections)
        // Followings of people I follow, excluding self and accounts I already follow
        var mutualCandidates = await _db.Follows
            .AsNoTracking()
            .Where(f => myFollowingIds.Contains(f.FollowerId) && f.FolloweeId != viewerId && !myFollowingIds.Contains(f.FolloweeId))
            .GroupBy(f => f.FolloweeId)
            .Select(g => new
            {
                CandidateId = g.Key,
                MutualCount = g.Count(),
                FirstMutualFriendId = g.Select(x => x.FollowerId).FirstOrDefault()
            })
            .ToListAsync(ct);

        var mutualMap = mutualCandidates.ToDictionary(x => x.CandidateId, x => (x.MutualCount, x.FirstMutualFriendId));

        // 3. Interaction candidates (Reactions or comments in the last 60 days)
        var interactionWindow = now.AddDays(-60);

        // Candidates whose posts viewer interacted with
        var viewerInteractedAuthorIds = await _db.Reactions
            .AsNoTracking()
            .Where(r => r.ProfileId == viewerId && r.PostId != null && r.CreatedAt >= interactionWindow)
            .Select(r => r.Post!.ProfileId)
            .Where(authorId => authorId != viewerId && !myFollowingIds.Contains(authorId))
            .Distinct()
            .ToListAsync(ct);

        // Candidates who interacted with viewer's posts
        var authorsInteractedWithViewer = await _db.Reactions
            .AsNoTracking()
            .Where(r => r.Post != null && r.Post.ProfileId == viewerId && r.ProfileId != viewerId && r.CreatedAt >= interactionWindow)
            .Select(r => r.ProfileId)
            .Where(authorId => !myFollowingIds.Contains(authorId))
            .Distinct()
            .ToListAsync(ct);

        var interactionCandidatesSet = viewerInteractedAuthorIds
            .Union(authorsInteractedWithViewer)
            .ToHashSet();

        // 4. Popular / Active Creator candidates (for cold start / serendipity)
        var popularCandidates = await _db.Profiles
            .AsNoTracking()
            .Where(p => p.Id != viewerId
                && !myFollowingIds.Contains(p.Id)
                && (!p.IsBanned || (p.BannedUntil != null && p.BannedUntil <= now))
                && p.PrivacyLevel != Favi_BE.Models.Enums.PrivacyLevel.Private)
            .OrderByDescending(p => p.LastActiveAt)
            .Take(50)
            .Select(p => p.Id)
            .ToListAsync(ct);

        // 5. Union all candidate IDs
        var allCandidateIds = mutualMap.Keys
            .Union(interactionCandidatesSet)
            .Union(popularCandidates)
            .Take(150)
            .ToList();

        if (allCandidateIds.Count == 0)
        {
            return [];
        }

        // 6. Fetch profiles in batch
        var profiles = await _db.Profiles
            .AsNoTracking()
            .Where(p => allCandidateIds.Contains(p.Id)
                && (!p.IsBanned || (p.BannedUntil != null && p.BannedUntil <= now)))
            .ToListAsync(ct);

        // 7. Batch load followers/following counts (avoids N+1)
        var followersCounts = await _db.Follows
            .AsNoTracking()
            .Where(f => allCandidateIds.Contains(f.FolloweeId))
            .GroupBy(f => f.FolloweeId)
            .Select(g => new { ProfileId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ProfileId, x => x.Count, ct);

        var followingCounts = await _db.Follows
            .AsNoTracking()
            .Where(f => allCandidateIds.Contains(f.FollowerId))
            .GroupBy(f => f.FollowerId)
            .Select(g => new { ProfileId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ProfileId, x => x.Count, ct);

        // 8. Resolve names of mutual friends for social proof
        var firstMutualFriendIds = mutualCandidates
            .Select(x => x.FirstMutualFriendId)
            .Distinct()
            .ToList();

        var mutualFriendNames = await _db.Profiles
            .AsNoTracking()
            .Where(p => firstMutualFriendIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.DisplayName ?? p.Username, ct);

        // 9. Score each candidate
        var scoredList = new List<(ProfileReadModel Model, double Score)>();
        foreach (var p in profiles)
        {
            var followersCount = followersCounts.GetValueOrDefault(p.Id, 0);
            var followingCount = followingCounts.GetValueOrDefault(p.Id, 0);

            var (mutualCount, firstFriendId) = mutualMap.GetValueOrDefault(p.Id, (0, Guid.Empty));
            var hasInteraction = interactionCandidatesSet.Contains(p.Id);
            var isActiveRecently = p.LastActiveAt.HasValue && p.LastActiveAt >= now.AddDays(-7);
            var hasAvatar = !string.IsNullOrWhiteSpace(p.AvatarUrl);
            var hasBio = !string.IsNullOrWhiteSpace(p.Bio);

            // Compute composite score
            double score = 0;
            score += mutualCount * 15.0;
            score += hasInteraction ? 6.0 : 0.0;
            score += isActiveRecently ? 3.0 : 0.0;
            score += Math.Log10(1 + followersCount) * 1.5;
            score += hasAvatar ? 2.0 : 0.0;
            score += hasBio ? 1.0 : 0.0;

            // Generate contextual recommendation reason
            string reason;
            if (mutualCount > 0 && mutualFriendNames.TryGetValue(firstFriendId, out var friendName))
            {
                reason = mutualCount == 1
                    ? $"Followed by {friendName}"
                    : $"Followed by {friendName} +{mutualCount - 1} mutual";
            }
            else if (hasInteraction)
            {
                reason = "Based on recent interactions";
            }
            else if (isActiveRecently && followersCount > 5)
            {
                reason = "Popular active creator";
            }
            else
            {
                reason = "Suggested for you";
            }

            var model = MapProfile(p, null, followersCount, followingCount, mutualCount, reason);
            scoredList.Add((model, score));
        }

        // 10. Order by score descending and paginate
        return scoredList
            .OrderByDescending(x => x.Score)
            .Skip(skip)
            .Take(take)
            .Select(x => x.Model)
            .ToList();
    }

    public async Task<IReadOnlyList<ProfileReadModel>> GetOnlineFriendsAsync(
        Guid profileId, int withinLastMinutes, CancellationToken ct = default)
    {
        var onlineThreshold = DateTime.UtcNow.AddMinutes(-withinLastMinutes);

        var followingIds = await _db.Follows
            .AsNoTracking()
            .Where(f => f.FollowerId == profileId)
            .Select(f => f.FolloweeId)
            .ToListAsync(ct);

        var result = new List<ProfileReadModel>();
        foreach (var friendId in followingIds)
        {
            var p = await _db.Profiles.AsNoTracking().FirstOrDefaultAsync(x => x.Id == friendId, ct);
            if (p is null) continue;
            if (!p.LastActiveAt.HasValue || p.LastActiveAt < onlineThreshold) continue;

            var followersCount = await _db.Follows.AsNoTracking().CountAsync(f => f.FolloweeId == p.Id, ct);
            var followingCount = await _db.Follows.AsNoTracking().CountAsync(f => f.FollowerId == p.Id, ct);
            result.Add(MapProfile(p, null, followersCount, followingCount));
        }

        return result.OrderByDescending(p => p.LastActiveAt).ToList();
    }

    public Task<string?> GetAvatarUrlAsync(Guid profileId, CancellationToken ct = default)
        => _db.PostMedias
            .AsNoTracking()
            .Where(m => m.ProfileId == profileId && m.IsAvatar)
            .OrderByDescending(m => m.Id)
            .Select(m => m.Url)
            .FirstOrDefaultAsync(ct);

    public Task<string?> GetPosterUrlAsync(Guid profileId, CancellationToken ct = default)
        => _db.PostMedias
            .AsNoTracking()
            .Where(m => m.ProfileId == profileId && m.IsPoster)
            .OrderByDescending(m => m.Id)
            .Select(m => m.Url)
            .FirstOrDefaultAsync(ct);

    private static ProfileReadModel MapProfile(
        Favi_BE.Models.Entities.Profile p,
        string? email,
        int followersCount,
        int followingCount,
        int mutualFriendsCount = 0,
        string? recommendationReason = null)
        => new(
            p.Id,
            p.Username,
            p.DisplayName,
            p.Bio,
            p.AvatarUrl,
            p.CoverUrl,
            email,
            p.CreatedAt,
            p.LastActiveAt ?? DateTime.MinValue,
            (int)p.PrivacyLevel,
            (int)p.FollowPrivacyLevel,
            p.IsBanned,
            p.BannedUntil,
            followersCount,
            followingCount,
            mutualFriendsCount,
            recommendationReason);
}
