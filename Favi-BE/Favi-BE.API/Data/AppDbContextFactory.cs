using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Favi_BE.Data;

/// <summary>EF tooling must not start the web host, migration/seed jobs or Redis workers.</summary>
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
        => new(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(
            Environment.GetEnvironmentVariable("ConnectionStrings__Default")
            ?? "Host=localhost;Database=favi_design_time;Username=favi").Options);
}
