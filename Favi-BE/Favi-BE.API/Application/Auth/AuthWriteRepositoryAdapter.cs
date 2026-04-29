using Favi_BE.Data;
using Favi_BE.Interfaces;
using Favi_BE.Models.Entities;
using Favi_BE.Models.Enums;
using Favi_BE.Modules.Auth.Application.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Favi_BE.API.Application.Auth;

/// <summary>
/// Implements IAuthWriteRepository (Auth module port) using the existing
/// IUnitOfWork + AppDbContext infrastructure. Keeps the module free of API dependencies.
/// </summary>
internal sealed class AuthWriteRepositoryAdapter : IAuthWriteRepository
{
    private readonly IUnitOfWork _uow;
    private readonly AppDbContext _db;

    public AuthWriteRepositoryAdapter(IUnitOfWork uow, AppDbContext db)
    {
        _uow = uow;
        _db = db;
    }

    public async Task<(AuthUserData? User, AuthCredentialData? Credential)> FindByEmailAsync(
        string email, CancellationToken ct = default)
    {
        var emailAccount = await _uow.EmailAccounts.GetByEmailAsync(email);
        if (emailAccount?.Profile is null)
            return (null, null);

        return (MapUser(emailAccount.Profile), new AuthCredentialData(emailAccount.Id, emailAccount.PasswordHash));
    }

    public async Task<(AuthUserData? User, AuthCredentialData? Credential)> FindByUsernameAsync(
        string username, CancellationToken ct = default)
    {
        var profile = await _uow.Profiles.GetByUsernameAsync(username);
        if (profile is null)
            return (null, null);

        var emailAccount = await _uow.EmailAccounts.GetByIdAsync(profile.Id);
        if (emailAccount is null)
            return (null, null);

        return (MapUser(profile), new AuthCredentialData(emailAccount.Id, emailAccount.PasswordHash));
    }

    public async Task<AuthUserData?> FindUserByIdAsync(Guid profileId, CancellationToken ct = default)
    {
        var profile = await _uow.Profiles.GetByIdAsync(profileId);
        return profile is null ? null : MapUser(profile);
    }

    public async Task<AuthCredentialData?> FindCredentialByProfileIdAsync(Guid profileId, CancellationToken ct = default)
    {
        var emailAccount = await _uow.EmailAccounts.GetByIdAsync(profileId);
        return emailAccount is null
            ? null
            : new AuthCredentialData(emailAccount.Id, emailAccount.PasswordHash);
    }

    public Task<bool> IsEmailUniqueAsync(string email, CancellationToken ct = default)
        => _uow.EmailAccounts.IsEmailUniqueAsync(email);

    public Task<bool> IsUsernameUniqueAsync(string username, CancellationToken ct = default)
        => _uow.Profiles.IsUsernameUniqueAsync(username);

    public async Task RegisterUserAsync(AuthUserRegistrationData data, CancellationToken ct = default)
    {
        var profile = new Profile
        {
            Id = data.Id,
            Username = data.Username,
            DisplayName = data.DisplayName,
            Role = UserRole.User,
            CreatedAt = DateTime.UtcNow,
            PrivacyLevel = PrivacyLevel.Public
        };

        var emailAccount = new EmailAccount
        {
            Id = data.Id,
            Email = data.Email,
            PasswordHash = data.PasswordHash,
            CreatedAt = DateTime.UtcNow
        };

        await _uow.Profiles.AddAsync(profile);
        await _uow.EmailAccounts.AddAsync(emailAccount);
    }

    public async Task UpdatePasswordHashAsync(Guid profileId, string newPasswordHash, CancellationToken ct = default)
    {
        var emailAccount = await _uow.EmailAccounts.GetByIdAsync(profileId);
        if (emailAccount is null) return;

        emailAccount.PasswordHash = newPasswordHash;
        _uow.EmailAccounts.Update(emailAccount);
    }

    public async Task UpdateLastActiveAsync(Guid profileId, CancellationToken ct = default)
    {
        var profile = await _uow.Profiles.GetByIdAsync(profileId);
        if (profile is null) return;

        profile.LastActiveAt = DateTime.UtcNow;
        _uow.Profiles.Update(profile);
    }

    public async Task SaveAsync(CancellationToken ct = default)
        => await _uow.CompleteAsync();

    private static AuthUserData MapUser(Profile p) =>
        new(p.Id, p.Username, p.DisplayName, p.AvatarUrl, p.Role.ToString().ToLower(), p.IsBanned);
}
