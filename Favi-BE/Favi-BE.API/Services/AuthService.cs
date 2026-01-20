using Favi_BE.Interfaces;
using Favi_BE.Interfaces.Repositories;
using Favi_BE.Interfaces.Services;
using Favi_BE.Models.Dtos;
using Favi_BE.Models.Entities;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace Favi_BE.Services;

public class AuthService : IAuthService
{
    private readonly IEmailAccountRepository _emailAccounts;
    private readonly IProfileRepository _profiles;
    private readonly IJwtService _jwtService;
    private readonly IUnitOfWork _unitOfWork;

    public AuthService(
        IEmailAccountRepository emailAccounts,
        IProfileRepository profiles,
        IJwtService jwtService,
        IUnitOfWork unitOfWork)
    {
        _emailAccounts = emailAccounts;
        _profiles = profiles;
        _jwtService = jwtService;
        _unitOfWork = unitOfWork;
    }

    public async Task<AuthResponse?> LoginAsync(string emailOrUsername, string password)
    {
        // Try to find user by email first, then by username
        EmailAccount? emailAccount = null;
        Profile? profile = null;

        // Check if input is an email
        if (emailOrUsername.Contains('@'))
        {
            emailAccount = await _emailAccounts.GetByEmailAsync(emailOrUsername);
            if (emailAccount != null)
            {
                profile = emailAccount.Profile;
            }
        }
        else
        {
            // Search by username
            profile = await _profiles.GetByUsernameAsync(emailOrUsername);
            if (profile != null)
            {
                emailAccount = await _emailAccounts.GetByIdAsync(profile.Id);
            }
        }

        if (emailAccount == null || profile == null)
            return null;

        // Verify password
        if (!BCrypt.Net.BCrypt.Verify(password, emailAccount.PasswordHash))
            return null;

        // Check if user is banned
        if (profile.IsBanned)
            return null;

        // Generate tokens
        var accessToken = _jwtService.CreateAccessToken(profile.Id, profile.Username, GetRoleString(profile.Role));
        var refreshToken = _jwtService.CreateRefreshToken(profile.Id, profile.Username, GetRoleString(profile.Role));

        return new AuthResponse(
            AccessToken: accessToken,
            RefreshToken: refreshToken,
            Message: "Login successful"
        );
    }

    public async Task<AuthResponse?> RegisterAsync(string email, string password, string username, string? displayName)
    {
        // Check if email is unique
        if (!await _emailAccounts.IsEmailUniqueAsync(email))
            return null;

        // Check if username is unique
        if (!await _profiles.IsUsernameUniqueAsync(username))
            return null;

        // Create profile
        var profile = new Profile
        {
            Id = Guid.NewGuid(),
            Username = username,
            DisplayName = displayName ?? username,
            Role = Models.Enums.UserRole.User,
            CreatedAt = DateTime.UtcNow,
            PrivacyLevel = Models.Enums.PrivacyLevel.Public
        };

        // Hash password
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

        // Create email account
        var emailAccount = new EmailAccount
        {
            Id = profile.Id,
            Email = email,
            PasswordHash = passwordHash,
            CreatedAt = DateTime.UtcNow
        };

        await _profiles.AddAsync(profile);
        await _emailAccounts.AddAsync(emailAccount);
        await _unitOfWork.CompleteAsync();

        // Generate tokens
        var accessToken = _jwtService.CreateAccessToken(profile.Id, profile.Username, GetRoleString(profile.Role));
        var refreshToken = _jwtService.CreateRefreshToken(profile.Id, profile.Username, GetRoleString(profile.Role));

        return new AuthResponse(
            AccessToken: accessToken,
            RefreshToken: refreshToken,
            Message: "Registration successful"
        );
    }

    public async Task<AuthResponse?> RefreshAsync(string refreshToken)
    {
        // Validate refresh token
        var principal = _jwtService.ValidateRefresh(refreshToken);
        if (principal == null)
            return null;

        // Extract profile ID from token
        var profileIdClaim = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (!Guid.TryParse(profileIdClaim, out var profileId))
            return null;

        // Get profile
        var profile = await _profiles.GetByIdAsync(profileId);
        if (profile == null || profile.IsBanned)
            return null;

        // Generate new access token
        var accessToken = _jwtService.CreateAccessToken(profile.Id, profile.Username, GetRoleString(profile.Role));

        return new AuthResponse(
            AccessToken: accessToken,
            RefreshToken: refreshToken,
            Message: "Token refreshed"
        );
    }

    // Helper methods
    private static string GetRoleString(Models.Enums.UserRole role)
    {
        return role.ToString().ToLower();
    }
}
