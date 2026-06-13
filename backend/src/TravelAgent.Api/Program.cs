using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using TravelAgent.Application;
using TravelAgent.Infrastructure;
using TravelAgent.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Render (and most container hosts) inject the listen port via PORT.
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port))
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        // Enums travel as camelCase strings ("morning", "midRange") in the API contract.
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter(System.Text.Json.JsonNamingPolicy.CamelCase)));
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();

// Behind Render's TLS-terminating proxy: trust X-Forwarded-* so the scheme and
// client IP (used by the rate limiter) reflect the real request.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

// Locked-down CORS for the deployed frontend origin(s); not needed in local
// dev where Vite proxies /api. Setting: Cors__AllowedOrigins (comma-separated).
var allowedOrigins = (builder.Configuration["Cors:AllowedOrigins"] ?? "")
    .Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
if (allowedOrigins.Length > 0)
{
    builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));
}

// AI calls are the expensive resource: cap them per client IP.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("ai", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
            }));
});

var app = builder.Build();

// Apply EF Core migrations on startup against PostgreSQL. Skipped for the
// Sqlite-backed test host (its provider isn't Npgsql).
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TravelAgentDbContext>();
    if (db.Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL")
        db.Database.Migrate();
}

app.UseForwardedHeaders();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    // Built-in OpenAPI document + Swagger UI on /swagger (development only).
    app.MapOpenApi();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "TravelAgent API v1"));
}

// No HTTPS redirect in-container: Render terminates TLS at the edge and
// forwards plain HTTP, so a redirect here would loop.
if (allowedOrigins.Length > 0) app.UseCors();
app.UseRateLimiter();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

// Exposed for WebApplicationFactory in integration tests.
public partial class Program;
