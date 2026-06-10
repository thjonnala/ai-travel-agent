using System.ClientModel;
using Azure.AI.OpenAI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
            options.UseSqlServer(connectionString, sql =>
                sql.EnableRetryOnFailure(maxRetryCount: 5)));
        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<TravelAgentDbContext>());

        // Demo mode: fixed local user. Replaced by a JWT-claims implementation
        // when Entra ID auth is enabled.
        services.AddScoped<ICurrentUserService, DemoCurrentUserService>();

        AddAiPlanner(services, configuration);

        return services;
    }

    private static void AddAiPlanner(IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(AzureOpenAiOptions.SectionName).Get<AzureOpenAiOptions>() ?? new AzureOpenAiOptions();

        if (options.IsConfigured)
        {
            // ChatClient is thread-safe; one instance per app.
            services.AddSingleton(_ =>
                new AzureOpenAIClient(new Uri(options.Endpoint), new ApiKeyCredential(options.ApiKey))
                    .GetChatClient(options.Deployment));
            services.AddScoped<IAiPlannerService, AzureOpenAiPlannerService>();
        }
        else
        {
            // No keys configured: keep the demo fully functional with a
            // deterministic planner instead of failing at startup.
            services.AddSingleton<IAiPlannerService, MockAiPlannerService>();
        }
    }
}
