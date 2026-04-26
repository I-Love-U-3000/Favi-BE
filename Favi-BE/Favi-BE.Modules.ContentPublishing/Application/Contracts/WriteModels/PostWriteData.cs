using Favi_BE.Modules.ContentPublishing.Domain;

namespace Favi_BE.Modules.ContentPublishing.Application.Contracts.WriteModels;

public sealed record PostWriteData(
    Guid Id,
    Guid ProfileId,
    string? Caption,
    PostPrivacy Privacy,
    string? LocationName,
    string? LocationFullAddress,
    double? LocationLatitude,
    double? LocationLongitude,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    bool IsArchived,
    DateTime? DeletedDayExpiredAt
);
