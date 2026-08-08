using Favi_BE.BuildingBlocks.Domain;

namespace Favi_BE.Modules.Auth.Domain;

public sealed class Profile : Entity
{
    public Guid Id { get; private set; }
    public string Username { get; private set; }
    public string? DisplayName { get; private set; }
    public string? AvatarUrl { get; private set; }
    public string? CoverUrl { get; private set; }
    public string? Bio { get; private set; }
    public string Role { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastActiveAt { get; private set; }
    public string PrivacyLevel { get; private set; }
    public string FollowPrivacyLevel { get; private set; }
    public bool IsBanned { get; private set; }
    public DateTime? BannedUntil { get; private set; }

    public Profile(
        Guid id,
        string username,
        string? displayName,
        string? avatarUrl,
        string? coverUrl,
        string? bio,
        string role,
        DateTime createdAt,
        DateTime? lastActiveAt,
        string privacyLevel,
        string followPrivacyLevel,
        bool isBanned,
        DateTime? bannedUntil)
    {
        Id = id;
        Username = username;
        DisplayName = displayName;
        AvatarUrl = avatarUrl;
        CoverUrl = coverUrl;
        Bio = bio;
        Role = role;
        CreatedAt = createdAt;
        LastActiveAt = lastActiveAt;
        PrivacyLevel = privacyLevel;
        FollowPrivacyLevel = followPrivacyLevel;
        IsBanned = isBanned;
        BannedUntil = bannedUntil;
    }

    public static Profile Create(Guid id, string username, string role, DateTime createdAt)
    {
        return new Profile(
            id,
            username,
            displayName: null,
            avatarUrl: null,
            coverUrl: null,
            bio: null,
            role,
            createdAt,
            lastActiveAt: null,
            privacyLevel: "Public",
            followPrivacyLevel: "Public",
            isBanned: false,
            bannedUntil: null);
    }

    public void Update(string? displayName, string? bio, string privacyLevel, string followPrivacyLevel)
    {
        DisplayName = displayName;
        Bio = bio;
        PrivacyLevel = privacyLevel;
        FollowPrivacyLevel = followPrivacyLevel;
    }

    public void Ban(DateTime? until)
    {
        IsBanned = true;
        BannedUntil = until;
    }

    public void Unban()
    {
        IsBanned = false;
        BannedUntil = null;
    }

    public void UpdateAvatar(string avatarUrl)
    {
        AvatarUrl = avatarUrl;
    }

    public void UpdateCover(string coverUrl)
    {
        CoverUrl = coverUrl;
    }

    public void UpdateLastActive(DateTime activeAt)
    {
        LastActiveAt = activeAt;
    }
}
