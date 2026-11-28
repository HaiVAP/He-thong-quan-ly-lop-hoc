using System.Text;
using System.Text.Json.Serialization;
using EduHub.Shared.Events;
using EduHub.Shared.Middlewares;
using EduHub.Contracts;
using EduHub.Api.Infrastructure;
using EduHub.Identity;
using EduHub.Identity.Infrastructure;
using EduHub.Academics;
using EduHub.Academics.Infrastructure;
using EduHub.Assignments;
using EduHub.Assignments.Infrastructure;
using EduHub.Communication;
using EduHub.Communication.Infrastructure;
using EduHub.Materials;
using EduHub.Materials.Infrastructure;
using EduHub.Statistics;
using EduHub.Api.Modules.Storage;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
var cfg = builder.Configuration;

// ==========================================
// 1. Controllers & JSON Options
// ==========================================
builder.Services.AddControllers()
    .AddApplicationPart(typeof(IdentityModuleExtensions).Assembly)
    .AddApplicationPart(typeof(AcademicsModuleExtensions).Assembly)
    .AddApplicationPart(typeof(AssignmentsModuleExtensions).Assembly)
    .AddApplicationPart(typeof(CommunicationModuleExtensions).Assembly)
    .AddApplicationPart(typeof(MaterialsModuleExtensions).Assembly)
    .AddApplicationPart(typeof(StatisticsModuleExtensions).Assembly)
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddEndpointsApiExplorer();

// ==========================================
// 2. Swagger với cấu hình JWT Bearer Auth
// ==========================================
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "EduHub API",
        Version = "v1",
        Description = "Backend API hệ thống Quản lý lớp học và nộp bài trực tuyến (Modular Monolith)"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Nhập Token theo cú pháp: Bearer {token}"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ==========================================
// 4. Xác thực & Phân quyền (JWT Bearer)
// ==========================================
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = cfg["Jwt:Issuer"],
            ValidAudience = cfg["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(cfg["Jwt:Secret"]!)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddScoped<IEventBus, InMemoryEventBus>();

builder.Services.AddIdentityModule(cfg);
builder.Services.AddAcademicsModule(cfg);
builder.Services.AddAssignmentsModule(cfg);
builder.Services.AddCommunicationModule(cfg);
builder.Services.AddMaterialsModule(cfg);
builder.Services.AddStatisticsModule();
builder.Services.AddSingleton<IStorageService, StorageService>();

// ==========================================
// 5. Health Checks & CORS
// ==========================================
builder.Services.AddHealthChecks();
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins(cfg["Cors:Frontend"] ?? "http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// ==========================================
// 6. Request Pipeline & Middleware
// ==========================================
app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseCors("Frontend");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "EduHub API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

// Root info endpoint
app.MapGet("/", () => Results.Ok(new
{
    name = "EduHub API",
    version = "1.0.0",
    status = "running",
    docs = "/swagger",
    health = "/health"
}));

// Auto migrate / seed database with retry.
static async Task MigrateLegacyMaterialsAsync(MaterialsDbContext context)
{
    await context.Database.ExecuteSqlRawAsync("""
        DO $$
        BEGIN
            IF to_regclass('public."MaterialCategories"') IS NOT NULL THEN
                INSERT INTO materials."MaterialCategories"
                    ("Id", "CreatedAt", "UpdatedAt", "ClassRoomId", "Name", "Description", "SortOrder")
                SELECT "Id", "CreatedAt", "UpdatedAt", "ClassRoomId", "Name", "Description", "SortOrder"
                FROM public."MaterialCategories"
                ON CONFLICT ("Id") DO NOTHING;

                PERFORM setval(
                    pg_get_serial_sequence('materials."MaterialCategories"', 'Id'),
                    GREATEST(COALESCE((SELECT MAX("Id") FROM materials."MaterialCategories"), 1), 1),
                    EXISTS(SELECT 1 FROM materials."MaterialCategories"));
            END IF;

            IF to_regclass('public."Materials"') IS NOT NULL THEN
                INSERT INTO materials."Materials"
                    ("Id", "CreatedAt", "UpdatedAt", "ClassRoomId", "CategoryId", "CreatedById",
                     "Title", "Description", "FileName", "FileKey", "FileUrl", "MimeType", "FileSizeBytes")
                SELECT "Id", "CreatedAt", "UpdatedAt", "ClassRoomId", "CategoryId", "CreatedById",
                       "Title", "Description", "FileName", "FileKey", "FileUrl", "MimeType", "FileSizeBytes"
                FROM public."Materials"
                ON CONFLICT ("Id") DO NOTHING;

                PERFORM setval(
                    pg_get_serial_sequence('materials."Materials"', 'Id'),
                    GREATEST(COALESCE((SELECT MAX("Id") FROM materials."Materials"), 1), 1),
                    EXISTS(SELECT 1 FROM materials."Materials"));
            END IF;
        END
        $$;
        """);
}

using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var identityDb = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
    var academicsDb = scope.ServiceProvider.GetRequiredService<AcademicsDbContext>();
    var assignmentsDb = scope.ServiceProvider.GetRequiredService<AssignmentsDbContext>();
    var communicationDb = scope.ServiceProvider.GetRequiredService<CommunicationDbContext>();
    var materialsDb = scope.ServiceProvider.GetRequiredService<MaterialsDbContext>();

    for (int i = 1; i <= 5; i++)
    {
        try
        {
            await identityDb.Database.MigrateAsync();
            await academicsDb.Database.MigrateAsync();
            await assignmentsDb.Database.MigrateAsync();
            await communicationDb.Database.MigrateAsync();
            await materialsDb.Database.MigrateAsync();
            await MigrateLegacyMaterialsAsync(materialsDb);
            await DemoSeeder.SeedAsync(identityDb, academicsDb, assignmentsDb, communicationDb, materialsDb);
            logger.LogInformation("Cơ sở dữ liệu EduHub và dữ liệu mẫu DemoSeeder đã sẵn sàng.");
            break;
        }
        catch (Exception ex)
        {
            logger.LogWarning("Đang chờ kết nối CSDL (Lần thử {Attempt}/5): {Message}", i, ex.Message);
            if (i == 5) throw;
            await Task.Delay(2000);
        }
    }
}

app.Run();
