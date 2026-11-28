using EduHub.Academics.Application.Services;
using EduHub.Academics.Infrastructure;
using EduHub.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EduHub.Academics;

public static class AcademicsModuleExtensions
{
    public static IServiceCollection AddAcademicsModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AcademicsDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "academics")));

        services.AddScoped<IDepartmentService, DepartmentService>();
        services.AddScoped<IClassService, ClassService>();
        services.AddScoped<IClassAccess, ClassAccess>();
        services.AddScoped<IModuleStatistics, AcademicsStatistics>();

        return services;
    }
}
