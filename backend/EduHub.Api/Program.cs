using System.Text;
using System.Text.Json.Serialization;
using EduHub.Api.Infrastructure;
using EduHub.Api.Modules.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
var cfg = builder.Configuration;
builder.Services.AddControllers().AddJsonOptions(o => o.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<EduHubDbContext>(o => o.UseNpgsql(cfg.GetConnectionString("DefaultConnection")));
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o =>
{
    o.TokenValidationParameters = new TokenValidationParameters {
        ValidateIssuer=true, ValidateAudience=true, ValidateLifetime=true, ValidateIssuerSigningKey=true,
        ValidIssuer=cfg["Jwt:Issuer"], ValidAudience=cfg["Jwt:Audience"],
        IssuerSigningKey=new SymmetricSecurityKey(Encoding.UTF8.GetBytes(cfg["Jwt:Secret"]!)),
        ClockSkew=TimeSpan.FromSeconds(30)
    };
});
builder.Services.AddAuthorization();
builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddCors(o => o.AddPolicy("Frontend", p => p.WithOrigins(cfg["Cors:Frontend"] ?? "http://localhost:5173").AllowAnyHeader().AllowAnyMethod()));
var app=builder.Build();
app.UseCors("Frontend");
if(app.Environment.IsDevelopment()){app.UseSwagger();app.UseSwaggerUI();}
app.UseAuthentication(); app.UseAuthorization(); app.MapControllers();
using(var scope=app.Services.CreateScope()){
 var db=scope.ServiceProvider.GetRequiredService<EduHubDbContext>();
 await db.Database.EnsureCreatedAsync();
 await DemoSeeder.SeedAsync(db);
}
app.MapGet("/",()=>Results.Ok(new{name="EduHub API",status="running"}));
app.Run();
