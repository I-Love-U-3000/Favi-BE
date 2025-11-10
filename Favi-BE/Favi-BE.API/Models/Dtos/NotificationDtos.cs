using Favi_BE.Models.Enums;

namespace Favi_BE.Models.Dtos
{
    public record NotificationDto(
        int Id,
        NotificationType Type,
        Guid ActorProfileId,
        Guid? TargetPostId,
        Guid? TargetCommentId,
        string Message,
        DateTime CreatedAt
    );
}
