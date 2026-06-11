using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy
        .AllowAnyOrigin()
        .AllowAnyHeader()
        .AllowAnyMethod();
    });
});

// Read JWT settings from configuration (appsettings.json / environment variables)
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("JWT Key is not configured.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException("JWT Issuer is not configured.");

// Add JWT authentication — the Gateway validates tokens before forwarding to downstream services
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = false
        };
    });

builder.Configuration
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddJsonFile("ocelot.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables();

builder.Services.AddOcelot(builder.Configuration);

var app = builder.Build();

// Log every incoming request for debugging
app.Use(async (context, next) =>
{
    Console.WriteLine($"[Gateway] {context.Request.Method} {context.Request.Path}");
    await next.Invoke();
});

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

// Root home page — short-circuits before Ocelot so it always responds at /
// Must be placed before app.UseOcelot() since Ocelot is the terminal middleware.
var gatewayStartTime = DateTime.UtcNow;
app.Use(async (context, next) =>
{
    if (context.Request.Path == "/")
    {
        var info = new
        {
            service     = "API Gateway (Ocelot)",
            status      = "running",
            startedAt   = gatewayStartTime,
            uptime      = $"{(DateTime.UtcNow - gatewayStartTime).TotalSeconds:F0}s",
            port        = 5000,
            description = "Single entry point for all client requests. Routes traffic to downstream services after JWT validation.",
            routes = new[]
            {
                new { upstream = "POST /auth/register",   downstream = "AuthService",    auth = false,  note = "Register a new user" },
                new { upstream = "POST /auth/login",      downstream = "AuthService",    auth = false,  note = "Login, returns JWT" },
                new { upstream = "POST /auth/validate",   downstream = "AuthService",    auth = false,  note = "Validate a JWT token" },
                new { upstream = "GET  /auth",            downstream = "AuthService",    auth = false,  note = "List all users" },
                new { upstream = "GET  /products",        downstream = "ProductService", auth = true,   note = "List all products" },
                new { upstream = "GET  /products/{id}",   downstream = "ProductService", auth = true,   note = "Get product by ID" },
                new { upstream = "POST /products",        downstream = "ProductService", auth = true,   note = "Create product (Admin)" },
                new { upstream = "PUT  /products/{id}",   downstream = "ProductService", auth = true,   note = "Update product (Admin)" },
                new { upstream = "DELETE /products/{id}", downstream = "ProductService", auth = true,   note = "Delete product (Admin)" },
                new { upstream = "GET  /orders",          downstream = "OrderService",   auth = true,   note = "List all orders" },
                new { upstream = "GET  /orders/{id}",     downstream = "OrderService",   auth = true,   note = "Get order by ID" },
                new { upstream = "POST /orders",          downstream = "OrderService",   auth = true,   note = "Place an order" },
                new { upstream = "DELETE /orders/{id}",   downstream = "OrderService",   auth = true,   note = "Cancel order (Admin)" }
            },
            tip = "All routes with auth=true require: Authorization: Bearer <JWT token from /auth/login>"
        };
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(info);
        return; // short-circuit: do not pass to Ocelot
    }
    await next.Invoke();
});

await app.UseOcelot();

Console.WriteLine("Ocelot gateway running...");

app.Run();