using System.ComponentModel.DataAnnotations;
using Favi_BE.Models.Enums;

namespace Favi_BE.Models.Dtos
{
    public record BanUserRequest(
        [Required(ErrorMessage = "Lý do là bắt buộc.")] string Reason,
        [Range(1, 3650, ErrorMessage = "Thời gian ban tối đa 10 năm.")] int? DurationDays
    );

    public record WarnUserRequest(
        [Required(ErrorMessage = "Lý do là bắt buộc.")] string Reason
    );

    public record UnbanUserRequest(string? Reason);

    public record UserModerationResponse(
        Guid Id,
        Guid ProfileId,
        ModerationActionType ActionType,
        string Reason,
        DateTime CreatedAt,
        DateTime? ExpiresAt,
        DateTime? RevokedAt,
        bool Active,
        Guid AdminActionId,
        Guid AdminId
    );

    public record AdminDeleteContentRequest(
        [Required(ErrorMessage = "Lý do là bắt buộc.")] string Reason
    );

    public record UserWarningsResponse(
        List<UserModerationResponse> Warnings,
        int TotalCount,
        int Page,
        int PageSize,
        int TotalPages
    );

    public record UserBanHistoryResponse(
        List<UserModerationResponse> Bans,
        int TotalCount,
        int Page,
        int PageSize,
        int TotalPages,
        UserModerationResponse? ActiveBan
    );
}
