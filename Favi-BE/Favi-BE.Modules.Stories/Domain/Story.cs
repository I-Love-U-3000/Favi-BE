using Favi_BE.BuildingBlocks.Domain;
using Favi_BE.Modules.Stories.Domain.Rules;

namespace Favi_BE.Modules.Stories.Domain;

public sealed class Story : Entity
{
    public Guid Id { get; private set; }
    public Guid ProfileId { get; private set; }
    public string? MediaUrl { get; private set; }
    public string? MediaPublicId { get; private set; }
    public int MediaWidth { get; private set; }
    public int MediaHeight { get; private set; }
    public string? MediaFormat { get; private set; }
    public string? ThumbnailUrl { get; private set; }
    public StoryPrivacy Privacy { get; private set; }
    public bool IsArchived { get; private set; }
    public bool IsNSFW { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }

    public Story(
        Guid id,
        Guid profileId,
        string? mediaUrl,
        string? mediaPublicId,
        int mediaWidth,
        int mediaHeight,
        string? mediaFormat,
        string? thumbnailUrl,
        StoryPrivacy privacy,
        bool isArchived,
        bool isNSFW,
        DateTime createdAt,
        DateTime expiresAt)
    {
        Id = id;
        ProfileId = profileId;
        MediaUrl = mediaUrl;
        MediaPublicId = mediaPublicId;
        MediaWidth = mediaWidth;
        MediaHeight = mediaHeight;
        MediaFormat = mediaFormat;
        ThumbnailUrl = thumbnailUrl;
        Privacy = privacy;
        IsArchived = isArchived;
        IsNSFW = isNSFW;
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;
    }

    public static Story Create(
        Guid id,
        Guid profileId,
        string? mediaUrl,
        string? mediaPublicId,
        int mediaWidth,
        int mediaHeight,
        string? mediaFormat,
        string? thumbnailUrl,
        StoryPrivacy privacy,
        DateTime createdAt,
        DateTime expiresAt)
    {
        CheckRule(new StoryTTL24hRule(createdAt, expiresAt));
        return new Story(
            id,
            profileId,
            mediaUrl,
            mediaPublicId,
            mediaWidth,
            mediaHeight,
            mediaFormat,
            thumbnailUrl,
            privacy,
            isArchived: false,
            isNSFW: false,
            createdAt,
            expiresAt);
    }
}
