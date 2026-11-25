using EduHub.Materials.Application.Services;
using EduHub.Materials.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EduHub.Materials;

public static class MaterialsModuleExtensions
{
    public static IServiceCollection AddMaterialsModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<MaterialsDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IMaterialService, MaterialService>();

        return services;
    }
}
