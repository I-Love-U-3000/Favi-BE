using System.Globalization;
using System.Text;
using Favi_BE.Data;
using Favi_BE.Services;
using Microsoft.EntityFrameworkCore;

namespace Favi_BE.API.Seed.Steps;

public sealed class SeedAuthBootstrapStep
{
    public async Task<SeedAuthBootstrapResult> ExecuteAsync(
        AppDbContext db,
        IJwtService jwtService,
        CancellationToken cancellationToken = default)
    {
        var users = await db.Profiles
            .AsNoTracking()
            .OrderBy(p => p.Id)
            .Select(p => new { p.Id, p.Username, p.Role })
            .ToListAsync(cancellationToken);

        if (users.Count == 0)
            throw new InvalidOperationException("Auth bootstrap requires seeded users.");

        var outputRoot = Path.GetFullPath(SeedConfig.OutputPaths.Root);
        Directory.CreateDirectory(outputRoot);
        var filePath = Path.Combine(outputRoot, "tokens.csv");

        using var stream = File.Create(filePath);
        using var writer = new StreamWriter(stream, new UTF8Encoding(false));

        writer.WriteLine("profile_id,username,token,generated_at");

        var generatedAt = DateTime.UtcNow;
        foreach (var user in users)
        {
            var role = user.Role.ToString().ToLowerInvariant();
            var token = jwtService.CreateAccessToken(user.Id, user.Username, role);
            writer.WriteLine(string.Create(
                CultureInfo.InvariantCulture,
                $"{user.Id},{user.Username},{EscapeCsv(token)},{generatedAt:O}"));
        }

        return new SeedAuthBootstrapResult(users.Count, filePath);
    }

    private static string EscapeCsv(string value)
    {
        if (value.Contains(',') || value.Contains('"'))
            return $"\"{value.Replace("\"", "\"\"")}\"";

        return value;
    }
}

public readonly record struct SeedAuthBootstrapResult(int UserCount, string ExportPath);
