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
        Guid[] ReadBy, // Array of profile IDs who have read this message
        PostPreviewDto? PostPreview // Post preview if this message contains a shared post
    );

    public record CreateDmConversationRequest(
        Guid OtherProfileId
    );

    public record CreateGroupConversationRequest(
        IEnumerable<Guid> MemberIds
    );

    public record SendMessageRequest(
        string? Content,
        string? MediaUrl,
        Guid? PostId
    );

    public record ChatImageUploadResponse(
        string Url,
        string PublicId,
        int Width,
        int Height,
        string Format
    );

    // Post preview data for shared posts in chat messages
    public record PostPreviewDto(
        Guid Id,
        Guid AuthorProfileId,
        string? Caption,
        string? ThumbnailUrl,
        int MediasCount,
        DateTime CreatedAt
    );
}
