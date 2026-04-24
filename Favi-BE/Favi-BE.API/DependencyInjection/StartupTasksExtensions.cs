using Favi_BE.Data;
using Microsoft.EntityFrameworkCore;

namespace Favi_BE.API.DependencyInjection;

public static class StartupTasksExtensions
{
    public static async Task ApplyDatabaseMigrationsAndSeedAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var appliedMigrations = await db.Database.GetAppliedMigrationsAsync();
        var hasMigrationHistory = appliedMigrations.Any();
        var hasTables = false;
        try
        {
            await db.Profiles.Take(1).ToListAsync();
            hasTables = true;
        }
        catch
        {
            hasTables = false;
        }

        if (hasTables && !hasMigrationHistory)
        {
            Console.WriteLine("[Migrate] Database is in inconsistent state (tables exist but no migration history).");
            Console.WriteLine("[Migrate] Dropping and recreating database...");
            await db.Database.EnsureDeletedAsync();
            Console.WriteLine("[Migrate] Database dropped. Creating fresh schema...");
        }

        var retries = 0;
        const int maxRetries = 10;
        while (true)
        {
            try
            {
                db.Database.Migrate();
                break;
            }
            catch (Exception ex) when (retries < maxRetries)
            {
                retries++;
                Console.WriteLine($"[Migrate] retry {retries}/{maxRetries}: {ex.Message}");
                await Task.Delay(2000);
            }
        }

        await Favi_BE.API.Seed.SeedPipeline.InitializeAsync(app.Services);
    }
}
