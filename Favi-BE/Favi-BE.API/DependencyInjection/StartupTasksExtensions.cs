using Favi_BE.Data;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;

namespace Favi_BE.API.DependencyInjection;

public static class StartupTasksExtensions
{
    public static async Task ApplyDatabaseMigrationsAndSeedAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Wait for database connection to be ready
        var connRetries = 0;
        const int maxConnRetries = 15;
        DbConnection connection = null!;
        while (true)
        {
            try
            {
                connection = db.Database.GetDbConnection();
                await connection.OpenAsync();
                break;
            }
            catch (Exception ex) when (connRetries < maxConnRetries)
            {
                connRetries++;
                Console.WriteLine($"[Startup] Waiting for PostgreSQL ({connRetries}/{maxConnRetries}): {ex.Message}");
                await Task.Delay(2000);
            }
        }

        // Acquire PostgreSQL Session-level Advisory Lock
        // Ensures only one replica runs migrations and seeding at a time during concurrent cold starts.
        Console.WriteLine("[Startup] Acquiring PostgreSQL advisory lock (742189)...");
        await using (var lockCmd = connection.CreateCommand())
        {
            lockCmd.CommandText = "SELECT pg_advisory_lock(742189);";
            await lockCmd.ExecuteNonQueryAsync();
        }
        Console.WriteLine("[Startup] Acquired PostgreSQL advisory lock.");

        try
        {
            var appliedMigrations = await db.Database.GetAppliedMigrationsAsync();
            var hasMigrationHistory = appliedMigrations.Any();
            var hasTables = false;
            try
            {
                await db.Profiles.OrderBy(p => p.Id).Take(1).ToListAsync();
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
        finally
        {
            try
            {
                await using var unlockCmd = connection.CreateCommand();
                unlockCmd.CommandText = "SELECT pg_advisory_unlock(742189);";
                await unlockCmd.ExecuteNonQueryAsync();
                Console.WriteLine("[Startup] Released PostgreSQL advisory lock.");
            }
            catch { }
            await connection.CloseAsync();
        }
    }
}

