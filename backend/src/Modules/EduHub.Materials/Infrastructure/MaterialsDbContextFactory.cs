using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace EduHub.Materials.Infrastructure;

public sealed class MaterialsDbContextFactory : IDesignTimeDbContextFactory<MaterialsDbContext>
{
    public MaterialsDbContext CreateDbContext(string[] args)
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
        var options = new DbContextOptionsBuilder<MaterialsDbContext>()
            .UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "materials"))
            .Options;

        return new MaterialsDbContext(options);
    }
}
