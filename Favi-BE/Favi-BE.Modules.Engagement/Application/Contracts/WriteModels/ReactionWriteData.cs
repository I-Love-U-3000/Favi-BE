using Favi_BE.Modules.Engagement.Domain;

namespace Favi_BE.Modules.Engagement.Application.Contracts.WriteModels;

public sealed record ReactionWriteData(
    Guid Id,
    Guid ProfileId,
    Guid? PostId,
    Guid? CommentId,
    Guid? CollectionId,
    Guid? RepostId,
    ReactionType Type,
    DateTime CreatedAt);
