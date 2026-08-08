using Favi_BE.BuildingBlocks.Domain;
using Favi_BE.Modules.ContentPublishing.Domain.Rules;

namespace Favi_BE.Modules.ContentPublishing.Domain;

public sealed class Post : Entity
{
    public Guid Id { get; private set; }
    public Guid ProfileId { get; private set; }
    public string? Caption { get; private set; }
    public PostPrivacy Privacy { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public string? LocationName { get; private set; }
    public string? LocationFullAddress { get; private set; }
    public double? LocationLatitude { get; private set; }
    public double? LocationLongitude { get; private set; }
    public DateTime? DeletedDayExpiredAt { get; private set; }
    public bool IsArchived { get; private set; }
    public bool IsNSFW { get; private set; }

    public Post(
        Guid id,
        Guid profileId,
        string? caption,
        PostPrivacy privacy,
        DateTime createdAt,
        DateTime updatedAt,
        string? locationName,
        string? locationFullAddress,
        double? locationLatitude,
        double? locationLongitude,
        DateTime? deletedDayExpiredAt,
        bool isArchived,
        bool isNSFW)
    {
        Id = id;
        ProfileId = profileId;
        Caption = caption;
        Privacy = privacy;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        LocationName = locationName;
        LocationFullAddress = locationFullAddress;
        LocationLatitude = locationLatitude;
        LocationLongitude = locationLongitude;
        DeletedDayExpiredAt = deletedDayExpiredAt;
        IsArchived = isArchived;
        IsNSFW = isNSFW;
    }

    public static Post Create(
        Guid id,
        Guid profileId,
        string? caption,
        PostPrivacy privacy,
        string? locationName,
        string? locationFullAddress,
        double? locationLatitude,
        double? locationLongitude)
    {
        var now = DateTime.UtcNow;
        return new Post(
            id,
            profileId,
            caption,
            privacy,
            now,
            now,
            locationName,
            locationFullAddress,
            locationLatitude,
            locationLongitude,
            deletedDayExpiredAt: null,
            isArchived: false,
            isNSFW: false);
    }

    public void Update(Guid profileId, string? caption, PostPrivacy privacy)
    {
        CheckRule(new OwnerOnlyMutateRule(profileId, ProfileId));
        Caption = caption;
        Privacy = privacy;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Archive(Guid profileId)
    {
        CheckRule(new OwnerOnlyMutateRule(profileId, ProfileId));
        IsArchived = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Unarchive(Guid profileId)
    {
        CheckRule(new OwnerOnlyMutateRule(profileId, ProfileId));
        IsArchived = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SoftDelete(Guid profileId)
    {
        CheckRule(new OwnerOnlyMutateRule(profileId, ProfileId));
        DeletedDayExpiredAt = DateTime.UtcNow.AddDays(30);
        UpdatedAt = DateTime.UtcNow;
    }

    public void Restore(Guid profileId)
    {
        CheckRule(new OwnerOnlyMutateRule(profileId, ProfileId));
        DeletedDayExpiredAt = null;
        UpdatedAt = DateTime.UtcNow;
    }
}
