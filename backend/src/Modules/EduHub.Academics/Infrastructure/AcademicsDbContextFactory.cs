using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace EduHub.Academics.Infrastructure;

public sealed class AcademicsDbContextFactory : IDesignTimeDbContextFactory<AcademicsDbContext>
{
    public AcademicsDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddJsonFile("EduHub.Api/appsettings.json", optional: true)
            .AddJsonFile("EduHub.Api/appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is required for EF migrations.");
        var options = new DbContextOptionsBuilder<AcademicsDbContext>()
            .UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "academics"))
            .Options;

        return new AcademicsDbContext(options);
    }
}
