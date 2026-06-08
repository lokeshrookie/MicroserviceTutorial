using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ProductService.Data;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register EF Core InMemory database for product catalog
builder.Services.AddDbContext<ProductDbContext>(options =>
    options.UseInMemoryDatabase("ProductDb"));

// Add JWT authentication — validates tokens issued by AuthService
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.ASCII.GetBytes(builder.Configuration["Jwt:Key"]
                    ?? throw new InvalidOperationException("Jwt:Key is not configured."))),
            ValidateIssuer = true,
            ValidIssuer    = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = false
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Seed the in-memory database on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
    db.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

// Root home page — confirms service is running and lists available endpoints
var productStartTime = DateTime.UtcNow;
app.MapGet("/", () => Results.Json(new
{
    service   = "ProductService",
    status    = "running",
    startedAt = productStartTime,
    uptime    = $"{(DateTime.UtcNow - productStartTime).TotalSeconds:F0}s",
    port      = 5001,
    endpoints = new[]
    {
        "GET    /api/products       — List all products        (roles: Admin, User)",
        "GET    /api/products/{id}  — Get product by ID        (roles: Admin, User)",
        "POST   /api/products       — Create a product         (roles: Admin)",
        "PUT    /api/products/{id}  — Update a product         (roles: Admin)",
        "DELETE /api/products/{id}  — Delete a product         (roles: Admin)",
        "GET    /health             — Docker health check"
    },
    note = "All endpoints require a valid JWT. Send: Authorization: Bearer <token>"
}));

app.MapControllers();

Console.WriteLine($"{DateTime.UtcNow} ProductService starting...");

app.Run();