using EduHub.Assignments.Application.Services;
using EduHub.Assignments.Infrastructure;
using EduHub.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EduHub.Assignments;

public static class AssignmentsModuleExtensions
{
    public static IServiceCollection AddAssignmentsModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AssignmentsDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "assignments")));

        services.AddScoped<IAssignmentService, AssignmentService>();
        services.AddScoped<IModuleStatistics, AssignmentsStatistics>();

        return services;
    }
}
