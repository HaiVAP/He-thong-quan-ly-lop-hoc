using EduHub.Assignments.Application.Services;
using EduHub.Assignments.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EduHub.Assignments;

public static class AssignmentsModuleExtensions
{
    public static IServiceCollection AddAssignmentsModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AssignmentsDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IAssignmentService, AssignmentService>();

        return services;
    }
}

