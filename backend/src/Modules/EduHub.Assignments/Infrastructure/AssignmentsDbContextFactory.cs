using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace EduHub.Assignments.Infrastructure;

public sealed class AssignmentsDbContextFactory : IDesignTimeDbContextFactory<AssignmentsDbContext>
{
    public AssignmentsDbContext CreateDbContext(string[] args)
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
        var options = new DbContextOptionsBuilder<AssignmentsDbContext>()
            .UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "assignments"))
            .Options;

        return new AssignmentsDbContext(options);
    }
}
