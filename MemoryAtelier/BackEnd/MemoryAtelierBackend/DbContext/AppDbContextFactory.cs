using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace MemoryAtelierBackend.Data;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        // Get the project directory - walk up from current directory to find .csproj
        var projectDir = Directory.GetCurrentDirectory();
        while (!Directory.GetFiles(projectDir, "*.csproj").Any() && projectDir != Path.GetPathRoot(projectDir))
        {
            projectDir = Directory.GetParent(projectDir)?.FullName ?? projectDir;
        }

        DotNetEnv.Env.Load(Path.Combine(projectDir, ".env"));

        var configuration = new ConfigurationBuilder()
            .SetBasePath(projectDir)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = new[]
        {
            configuration.GetConnectionString("Default"),
            configuration["Supabase:DatabaseUrl"],
            configuration["DATABASE_URL"]
        }.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Database connection string is not configured for design-time operations.");
        }

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new AppDbContext(optionsBuilder.Options);
    }
}
