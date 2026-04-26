namespace Favi_BE.Modules.ContentPublishing.Application.Contracts.WriteModels;

public sealed record RepostWriteData(
    Guid Id,
    Guid ProfileId,
    Guid OriginalPostId,
    string? Caption,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
