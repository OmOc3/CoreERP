using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace ERP.Infrastructure.Persistence;

public sealed class ErpDbContextFactory : IDesignTimeDbContextFactory<ErpDbContext>
{
    public ErpDbContext CreateDbContext(string[] args)
    {
        var basePath = ResolveConfigurationBasePath();
        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Server=localhost,1433;Database=ERPDb;User Id=sa;Password=Your_password123;TrustServerCertificate=True;Encrypt=False;MultipleActiveResultSets=True";
        var databaseProvider = configuration["ERP:DatabaseProvider"]?.Trim().ToLowerInvariant() ?? "sqlserver";

        var optionsBuilder = new DbContextOptionsBuilder<ErpDbContext>();
        if (databaseProvider == "sqlite")
        {
            optionsBuilder.UseSqlite(connectionString);
        }
        else
        {
            optionsBuilder.UseSqlServer(connectionString);
        }

        return new ErpDbContext(optionsBuilder.Options);
    }

    private static string ResolveConfigurationBasePath()
    {
        var current = new DirectoryInfo(Directory.GetCurrentDirectory());

        while (current != null)
        {
            var apiDirectory = Path.Combine(current.FullName, "src", "ERP.Api");
            if (Directory.Exists(apiDirectory))
            {
                return apiDirectory;
            }

            current = current.Parent;
        }

        return Directory.GetCurrentDirectory();
    }
}
