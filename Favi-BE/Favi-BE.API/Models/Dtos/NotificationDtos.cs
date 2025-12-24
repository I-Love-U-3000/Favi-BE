using Favi_BE.Models.Enums;

namespace Favi_BE.Models.Dtos
{
    public record NotificationDto(
        Guid Id,
        NotificationType Type,
        Guid ActorProfileId,
        string ActorUsername,
        string? ActorDisplayName,
        string? ActorAvatarUrl,
        Guid? TargetPostId,
        Guid? TargetCommentId,
        string Message,
        bool IsRead,
        DateTime CreatedAt
    );
}
