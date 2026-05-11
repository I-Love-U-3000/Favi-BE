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
        var profiles = await _db.Profiles.AsNoTracking().ToListAsync(ct);

        var followedIds = await _db.Follows
            .AsNoTracking()
            .Where(f => f.FollowerId == viewerId)
            .Select(f => f.FolloweeId)
            .ToHashSetAsync(ct);

        var result = new List<ProfileReadModel>();
        foreach (var p in profiles)
        {
            if (p.Id == viewerId) continue;
            if (followedIds.Contains(p.Id)) continue;

            var followersCount = await _db.Follows.AsNoTracking().CountAsync(f => f.FolloweeId == p.Id, ct);
            var followingCount = await _db.Follows.AsNoTracking().CountAsync(f => f.FollowerId == p.Id, ct);
            result.Add(MapProfile(p, null, followersCount, followingCount));
        }

        return result.Skip(skip).Take(take).ToList();
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
        Favi_BE.Models.Entities.Profile p, string? email, int followersCount, int followingCount)
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
            followingCount);
}
