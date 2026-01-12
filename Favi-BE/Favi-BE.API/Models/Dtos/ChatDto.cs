using Favi_BE.API.Models.Enums;

namespace Favi_BE.API.Models.Dtos
{
    public record ConversationMemberDto(
        Guid ProfileId,
        string Username,
        string? DisplayName,
        string? AvatarUrl,
        DateTime? LastActiveAt
    );

    public record ConversationSummaryDto(
        Guid Id,
        ConversationType Type,
        DateTime? LastMessageAt,
        string? LastMessagePreview,
        int UnreadCount,
        IEnumerable<ConversationMemberDto> Members
    );

    public record MessageDto(
        Guid Id,
        Guid ConversationId,
        Guid SenderId,
        string Username,
        string? DisplayName,
        string? AvatarUrl,
        string? Content,
        string? MediaUrl,
        DateTime CreatedAt,
        DateTime? UpdatedAt,
        bool IsEdited,
        Guid[] ReadBy // Array of profile IDs who have read this message
    );

    public record CreateDmConversationRequest(
        Guid OtherProfileId
    );

    public record CreateGroupConversationRequest(
        IEnumerable<Guid> MemberIds
    );

    public record SendMessageRequest(
        string? Content,
        string? MediaUrl
    );
}
