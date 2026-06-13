using System.ClientModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using OpenAI;
using OpenAI.Chat;
using TravelAgent.Application.Common;
using TravelAgent.Application.Planning;
using TravelAgent.Infrastructure.Ai;
using TravelAgent.Infrastructure.Identity;
using TravelAgent.Infrastructure.Persistence;

namespace TravelAgent.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registers all infrastructure services (database, AI planner, identity).
    /// Keeps the composition root in the API thin.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("TravelAgentDb");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Connection string 'TravelAgentDb' is not configured.");

        services.AddDbContext<TravelAgentDbContext>(options =>
            options.UseNpgsql(NormalizePostgresConnectionString(connectionString), npgsql =>
                npgsql.EnableRetryOnFailure(maxRetryCount: 5)));
        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<TravelAgentDbContext>());

        // Demo mode: fixed local user. Replaced by a JWT-claims implementation
        // when real auth is enabled.
        services.AddScoped<ICurrentUserService, DemoCurrentUserService>();

        AddAiPlanner(services, configuration);

        return services;
    }

    private static void AddAiPlanner(IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(AiOptions.SectionName).Get<AiOptions>() ?? new AiOptions();

        if (options.IsConfigured)
        {
            // ChatClient is thread-safe; one instance per app. The endpoint makes
            // this work with any OpenAI-compatible provider (Groq, OpenRouter, …).
            services.AddSingleton(_ => new ChatClient(
                model: options.Model,
                credential: new ApiKeyCredential(options.ApiKey),
                options: new OpenAIClientOptions { Endpoint = new Uri(options.Endpoint) }));
            services.AddScoped<IAiPlannerService, OpenAiCompatiblePlannerService>();
        }
        else
        {
            // No AI provider configured: keep the app fully functional with a
            // deterministic planner instead of failing at startup.
            services.AddSingleton<IAiPlannerService, MockAiPlannerService>();
        }
    }

    /// <summary>
    /// Render (and many managed Postgres hosts) hand out a "postgres://user:pass@host/db"
    /// URL, which Npgsql doesn't parse. Convert it to the key-value form Npgsql expects;
    /// pass through anything already in key-value format unchanged.
    /// </summary>
    private static string NormalizePostgresConnectionString(string raw)
    {
        if (!raw.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
            && !raw.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
            return raw;

        var uri = new Uri(raw);
        var userInfo = uri.UserInfo.Split(':', 2);
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.IsDefaultPort ? 5432 : uri.Port,
            Database = uri.AbsolutePath.TrimStart('/'),
            Username = Uri.UnescapeDataString(userInfo[0]),
            Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "",
            // Managed Postgres requires TLS; trust the provider-managed certificate.
            SslMode = SslMode.Require,
            TrustServerCertificate = true,
        };
        return builder.ConnectionString;
    }
}
