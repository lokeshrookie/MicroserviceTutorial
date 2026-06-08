using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OrderService.Data;
using OrderService.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register EF Core InMemory database for orders
builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseInMemoryDatabase("OrderDb"));

// Typed HttpClient for inter-service communication with ProductService.
// Base address is read from configuration so it works both locally and in Docker.
// In Docker: http://productservice (container name resolved by Docker DNS)
var productServiceUrl = builder.Configuration["Services:ProductService"]
    ?? "http://productservice";

builder.Services.AddHttpClient<IProductServiceClient, ProductServiceClient>(client =>
{
    client.BaseAddress = new Uri(productServiceUrl);
});

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
            ValidateIssuer   = true,
            ValidIssuer      = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = false
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Seed the in-memory database on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
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
var orderStartTime = DateTime.UtcNow;
app.MapGet("/", () => Results.Json(new
{
    service   = "OrderService",
    status    = "running",
    startedAt = orderStartTime,
    uptime    = $"{(DateTime.UtcNow - orderStartTime).TotalSeconds:F0}s",
    port      = 5002,
    endpoints = new[]
    {
        "GET    /api/orders       — List all orders             (roles: Admin, User)",
        "GET    /api/orders/{id}  — Get order by ID             (roles: Admin, User)",
        "POST   /api/orders       — Place an order              (roles: Admin, User) — validates ProductId via ProductService",
        "DELETE /api/orders/{id}  — Cancel an order (soft)      (roles: Admin)",
        "GET    /health           — Docker health check"
    },
    interServiceCommunication = "POST /api/orders calls ProductService internally to validate ProductId before saving",
    note = "All endpoints require a valid JWT. Send: Authorization: Bearer <token>"
}));

app.MapControllers();

Console.WriteLine($"{DateTime.UtcNow} OrderService starting...");

app.Run();