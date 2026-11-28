using EduHub.Communication.Application.EventHandlers;
using EduHub.Communication.Application.Services;
using EduHub.Communication.Infrastructure;
using EduHub.Contracts;
using EduHub.Contracts.Events;
using EduHub.Shared.Events;
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

        services.AddScoped<IEventHandler<StudentJoinedClassEvent>, StudentJoinedClassEventHandler>();
        services.AddScoped<IEventHandler<AssignmentCreatedEvent>, AssignmentCreatedEventHandler>();
        services.AddScoped<IEventHandler<SubmissionSubmittedEvent>, SubmissionSubmittedEventHandler>();
        services.AddScoped<IEventHandler<SubmissionGradedEvent>, SubmissionGradedEventHandler>();
        services.AddScoped<IEventHandler<MaterialUploadedEvent>, MaterialUploadedEventHandler>();

        return services;
    }
}
