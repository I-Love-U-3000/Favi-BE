using Favi_BE.Models.Enums;

namespace Favi_BE.Models.Dtos
{
    public record CreateCollectionRequest(
        Guid OwnerProfileId,
        string Title,
        string? Description,
        PrivacyLevel PrivacyLevel,
        string? CoverImageUrl
    );

    public record UpdateCollectionRequest(
        string? Title,
        string? Description,
        PrivacyLevel? PrivacyLevel,
        string? CoverImageUrl
    );

    public record CollectionResponse(
        Guid Id,
        Guid OwnerProfileId,
        string Title,
        string? Description,
        string CoverImageUrl,
        PrivacyLevel PrivacyLevel,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        IEnumerable<Guid> PostIds,
        int PostCount
    );

}
