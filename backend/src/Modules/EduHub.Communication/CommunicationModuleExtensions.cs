using EduHub.Communication.Application.Services;
using EduHub.Communication.Infrastructure;
using EduHub.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EduHub.Communication;

public static class CommunicationModuleExtensions
{
    public static IServiceCollection AddCommunicationModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<CommunicationDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "communication")));

        services.AddScoped<IAnnouncementService, AnnouncementService>();
        services.AddScoped<IModuleStatistics, CommunicationStatistics>();

        return services;
    }
}
