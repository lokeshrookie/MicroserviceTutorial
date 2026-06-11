using AuthService.Data;
using AuthService.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IAuthService, AuthService.Services.AuthService>();


// Add Swagger configuration
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Auth Service", Version = "v1" });
});

builder.Services.AddControllers();


var app = builder.Build();

// Enable Swagger only in Development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Auth Service v1"));
}
// Initialize database with seed data
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    dbContext.Database.Migrate();
    //dbContext.Database.EnsureCreated();
}

app.UseAuthentication();
app.UseAuthorization();

// Root home page — confirms service is running and lists available endpoints
var authStartTime = DateTime.UtcNow;
app.MapGet("/", () => Results.Json(new
{
    service   = "AuthService",
    status    = "running",
    startedAt = authStartTime,
    uptime    = $"{(DateTime.UtcNow - authStartTime).TotalSeconds:F0}s",
    port      = 5003,
    endpoints = new[]
    {
        "POST /api/auth/register  — Register a new user (returns JWT)",
        "POST /api/auth/login     — Login with credentials (returns JWT)",
        "POST /api/auth/validate  — Validate a JWT token",
        "GET  /api/auth           — List all registered users",
        "GET  /health             — Docker health check"
    },
    note = "Protected endpoints on other services require: Authorization: Bearer <token>"
}));

app.MapControllers();

app.Run();