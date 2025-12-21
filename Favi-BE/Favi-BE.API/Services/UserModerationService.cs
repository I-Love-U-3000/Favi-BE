using Favi_BE.Interfaces;
using Favi_BE.Interfaces.Services;
using Favi_BE.Models.Dtos;
using Favi_BE.Models.Entities;
using Favi_BE.Models.Enums;
using Microsoft.Extensions.Logging;

namespace Favi_BE.Services;

public class UserModerationService : IUserModerationService
{
    private readonly IUnitOfWork _uow;
    private readonly IAuditService _auditService;
    private readonly ILogger<UserModerationService> _logger;

    public UserModerationService(
        IUnitOfWork uow,
        IAuditService auditService,
        ILogger<UserModerationService> logger)
    {
        _uow = uow;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<UserModerationResponse?> BanAsync(Guid profileId, Guid adminId, BanUserRequest request)
    {
        var profile = await _uow.Profiles.GetByIdAsync(profileId);
        if (profile is null)
            return null;

        if (request.DurationDays.HasValue && request.DurationDays <= 0)
            throw new ArgumentException("DurationDays phải lớn hơn 0.", nameof(request.DurationDays));

        var existingBan = await _uow.UserModerations.GetActiveModerationAsync(profileId, ModerationActionType.Ban);
        if (existingBan is not null)
        {
            existingBan.Active = false;
            existingBan.RevokedAt = DateTime.UtcNow;
            _uow.UserModerations.Update(existingBan);
        }

        var expiresAt = request.DurationDays.HasValue
            ? DateTime.UtcNow.AddDays(request.DurationDays.Value)
            : (DateTime?)null;

        profile.IsBanned = true;
        profile.BannedUntil = expiresAt;

        var adminAction = await _auditService.LogUserActionAsync(
            adminId,
            AdminActionType.BanUser,
            profile.Id,
            request.Reason,
            saveChanges: false);

        var moderation = new UserModeration
        {
            Id = Guid.NewGuid(),
            ProfileId = profile.Id,
            AdminId = adminId,
            AdminActionId = adminAction.Id,
            ActionType = ModerationActionType.Ban,
            Reason = request.Reason,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = expiresAt,
            Active = true
        };

        await _uow.UserModerations.AddAsync(moderation);
        await _uow.CompleteAsync();

        return MapToResponse(moderation);
    }

    public async Task<UserModerationResponse?> WarnAsync(Guid profileId, Guid adminId, WarnUserRequest request)
    {
        var profile = await _uow.Profiles.GetByIdAsync(profileId);
        if (profile is null)
            return null;

        var adminAction = await _auditService.LogUserActionAsync(
            adminId,
            AdminActionType.WarnUser,
            profile.Id,
            request.Reason,
            saveChanges: false);

        var moderation = new UserModeration
        {
            Id = Guid.NewGuid(),
            ProfileId = profile.Id,
            AdminId = adminId,
            AdminActionId = adminAction.Id,
            ActionType = ModerationActionType.Warn,
            Reason = request.Reason,
            CreatedAt = DateTime.UtcNow,
            Active = true
        };

        await _uow.UserModerations.AddAsync(moderation);
        await _uow.CompleteAsync();
        return MapToResponse(moderation);
    }

    public async Task<bool> UnbanAsync(Guid profileId, Guid adminId, string? reason)
    {
        var profile = await _uow.Profiles.GetByIdAsync(profileId);
        if (profile is null)
            return false;

        if (!IsBanActive(profile))
            return false;

        profile.IsBanned = false;
        profile.BannedUntil = null;

        var activeBan = await _uow.UserModerations.GetActiveModerationAsync(profileId, ModerationActionType.Ban);
        if (activeBan is not null)
        {
            activeBan.Active = false;
            activeBan.RevokedAt = DateTime.UtcNow;
            _uow.UserModerations.Update(activeBan);
        }

        await _auditService.LogUserActionAsync(
            adminId,
            AdminActionType.UnbanUser,
            profile.Id,
            reason,
            saveChanges: false);

        await _uow.CompleteAsync();
        return true;
    }

    private static bool IsBanActive(Profile profile)
    {
        if (!profile.IsBanned)
            return false;

        if (!profile.BannedUntil.HasValue)
            return true;

        return profile.BannedUntil > DateTime.UtcNow;
    }

    private static UserModerationResponse MapToResponse(UserModeration moderation) =>
        new(
            moderation.Id,
            moderation.ProfileId,
            moderation.ActionType,
            moderation.Reason,
            moderation.CreatedAt,
            moderation.ExpiresAt,
            moderation.RevokedAt,
            moderation.Active,
            moderation.AdminActionId,
            moderation.AdminId);
}
