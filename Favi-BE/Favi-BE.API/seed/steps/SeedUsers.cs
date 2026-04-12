using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Favi_BE.Data;
using Favi_BE.Models.Entities;
using Favi_BE.Models.Enums;

namespace Favi_BE.API.Seed.Steps;

public sealed class SeedUsersStep
{
    private const string DefaultPassword = "123456";
    private const string DeterministicBcryptSalt = "$2a$11$abcdefghijklmnopqrstuu";
    private static readonly DateTime SeedAnchorUtc = new(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly string[] AvatarTags = ["portrait", "people", "face", "profile", "person"];
    private static readonly string[] CoverTags = ["landscape", "city", "nature", "travel", "lifestyle", "technology"];

    public async Task<SeedUsersResult> ExecuteAsync(
        AppDbContext db,
        SeedContext seedContext,
        CancellationToken cancellationToken = default)
    {
        var targetUserCount = seedContext.Random.Next(SeedConfig.Users.Min, SeedConfig.Users.Max + 1);

        var records = GenerateUsers(seedContext, targetUserCount);
        ValidateUsers(records, targetUserCount);

        var profiles = records.Select(r => r.Profile).ToList();
        var emailAccounts = records.Select(r => r.EmailAccount).ToList();

        await db.Profiles.AddRangeAsync(profiles, cancellationToken);
        await db.EmailAccounts.AddRangeAsync(emailAccounts, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        var exportPath = ExportUsersCsv(records);

        return new SeedUsersResult(profiles.Count, exportPath);
    }

    private static List<SeedUserRecord> GenerateUsers(SeedContext seedContext, int targetUserCount)
    {
        var records = new List<SeedUserRecord>(targetUserCount);

        var lurkerCount = (int)Math.Round(targetUserCount * SeedConfig.UserRoleDistribution["lurker"], MidpointRounding.AwayFromZero);
        var casualCount = (int)Math.Round(targetUserCount * SeedConfig.UserRoleDistribution["casual"], MidpointRounding.AwayFromZero);
        var powerCount = targetUserCount - lurkerCount - casualCount;

        var activityRoles = Enumerable.Repeat("lurker", lurkerCount)
            .Concat(Enumerable.Repeat("casual", casualCount))
            .Concat(Enumerable.Repeat("power", powerCount))
            .ToArray();

        for (var i = 0; i < targetUserCount; i++)
        {
            var profileId = DeterministicGuid(seedContext.SeedKey, i);
            var username = $"user_{i + 1:D5}";
            var email = $"{username}@seed.local";
            var activityRole = activityRoles[i];

            var createdAt = BuildCreatedAt(seedContext);
            var lastActiveAt = BuildLastActiveAt(seedContext, activityRole, createdAt);
            var role = BuildAccountRole(activityRole, i);

            var profile = new Profile
            {
                Id = profileId,
                Username = username,
                DisplayName = $"User {i + 1:D5}",
                AvatarUrl = BuildAvatarUrl(i),
                CoverUrl = BuildCoverUrl(i),
                Bio = $"Seeded benchmark profile {i + 1:D5}",
                Role = role,
                PrivacyLevel = BuildPrivacyLevel(seedContext),
                FollowPrivacyLevel = BuildFollowPrivacyLevel(seedContext),
                IsBanned = false,
                CreatedAt = createdAt,
                LastActiveAt = lastActiveAt
            };

            var emailAccount = new EmailAccount
            {
                Id = profileId,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(DefaultPassword, DeterministicBcryptSalt),
                CreatedAt = createdAt,
                EmailVerifiedAt = createdAt.AddMinutes(5)
            };

            records.Add(new SeedUserRecord(profile, emailAccount, activityRole));
        }

        return records;
    }

    private static UserRole BuildAccountRole(string activityRole, int index)
    {
        if (index == 0)
            return UserRole.Admin;

        if (activityRole == "power" && index % 18 == 0)
            return UserRole.Moderator;

        if (activityRole == "casual" && index % 400 == 0)
            return UserRole.Moderator;

        return UserRole.User;
    }

    private static string BuildAvatarUrl(int index)
    {
        var tag = AvatarTags[index % AvatarTags.Length];
        return $"https://loremflickr.com/320/320/{tag}?lock=avatar-{index + 1}";
    }

    private static string BuildCoverUrl(int index)
    {
        var tag = CoverTags[index % CoverTags.Length];
        return $"https://loremflickr.com/1280/420/{tag}?lock=cover-{index + 1}";
    }

    private static DateTime BuildCreatedAt(SeedContext seedContext)
    {
        var daysAgo = seedContext.Random.Next(30, 91);
        var hours = seedContext.Random.Next(0, 24);
        var minutes = seedContext.Random.Next(0, 60);
        return SeedAnchorUtc.AddDays(-daysAgo).AddHours(-hours).AddMinutes(-minutes);
    }

    private static DateTime BuildLastActiveAt(SeedContext seedContext, string activityRole, DateTime createdAt)
    {
        var inactiveWindow = activityRole switch
        {
            "lurker" => seedContext.Random.Next(7, 31),
            "casual" => seedContext.Random.Next(2, 14),
            "power" => seedContext.Random.Next(0, 3),
            _ => seedContext.Random.Next(1, 7)
        };

        var candidate = SeedAnchorUtc.AddDays(-inactiveWindow)
            .AddHours(-seedContext.Random.Next(0, 24))
            .AddMinutes(-seedContext.Random.Next(0, 60));

        return candidate < createdAt ? createdAt.AddMinutes(1) : candidate;
    }

    private static PrivacyLevel BuildPrivacyLevel(SeedContext seedContext)
    {
        var roll = seedContext.Random.NextDouble();
        if (roll < 0.75) return PrivacyLevel.Public;
        if (roll < 0.95) return PrivacyLevel.Followers;
        return PrivacyLevel.Private;
    }

    private static PrivacyLevel BuildFollowPrivacyLevel(SeedContext seedContext)
    {
        var roll = seedContext.Random.NextDouble();
        if (roll < 0.80) return PrivacyLevel.Public;
        if (roll < 0.95) return PrivacyLevel.Followers;
        return PrivacyLevel.Private;
    }

    private static Guid DeterministicGuid(string seedKey, int index)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{seedKey}:profile:{index}"));
        var guidBytes = new byte[16];
        Array.Copy(bytes, guidBytes, guidBytes.Length);
        return new Guid(guidBytes);
    }

    private static void ValidateUsers(IReadOnlyList<SeedUserRecord> records, int expectedCount)
    {
        if (records.Count != expectedCount)
            throw new InvalidOperationException("Validation failed: users count mismatch.");

        var duplicateUsername = records
            .GroupBy(r => r.Profile.Username, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(g => g.Count() > 1);
        if (duplicateUsername is not null)
            throw new InvalidOperationException($"Validation failed: duplicate username '{duplicateUsername.Key}'.");

        var duplicateEmail = records
            .GroupBy(r => r.EmailAccount.Email, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(g => g.Count() > 1);
        if (duplicateEmail is not null)
            throw new InvalidOperationException($"Validation failed: duplicate email '{duplicateEmail.Key}'.");

        if (records.Any(r => string.IsNullOrWhiteSpace(r.Profile.AvatarUrl)))
            throw new InvalidOperationException("Validation failed: profile avatar URL is empty.");

        if (records.Any(r => string.IsNullOrWhiteSpace(r.Profile.CoverUrl)))
            throw new InvalidOperationException("Validation failed: profile cover URL is empty.");

        var roleSet = records.Select(r => r.Profile.Role).Distinct().ToHashSet();
        if (!roleSet.Contains(UserRole.Admin) || !roleSet.Contains(UserRole.Moderator) || !roleSet.Contains(UserRole.User))
            throw new InvalidOperationException("Validation failed: account roles are not properly varied.");
    }

    private static string ExportUsersCsv(IEnumerable<SeedUserRecord> records)
    {
        var outputRoot = Path.GetFullPath(SeedConfig.OutputPaths.Root);
        Directory.CreateDirectory(outputRoot);

        var filePath = Path.Combine(outputRoot, "users.csv");

        using var stream = File.Create(filePath);
        using var writer = new StreamWriter(stream, new UTF8Encoding(false));

        writer.WriteLine("profile_id,username,display_name,email,password,role,activity_role,avatar_url,cover_url,privacy_level,follow_privacy_level,is_banned,created_at,last_active_at");

        foreach (var record in records)
        {
            var profile = record.Profile;
            var emailAccount = record.EmailAccount;

            writer.WriteLine(string.Create(
                CultureInfo.InvariantCulture,
                $"{profile.Id},{profile.Username},{profile.DisplayName},{emailAccount.Email},{DefaultPassword},{profile.Role},{record.ActivityRole},{EscapeCsv(profile.AvatarUrl)},{EscapeCsv(profile.CoverUrl)},{profile.PrivacyLevel},{profile.FollowPrivacyLevel},{profile.IsBanned},{profile.CreatedAt:O},{profile.LastActiveAt:O}"));
        }

        return filePath;
    }

    private sealed record SeedUserRecord(Profile Profile, EmailAccount EmailAccount, string ActivityRole);

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        if (value.Contains(',') || value.Contains('"'))
            return $"\"{value.Replace("\"", "\"\"")}\"";

        return value;
    }
}

public readonly record struct SeedUsersResult(int CreatedCount, string ExportPath);
