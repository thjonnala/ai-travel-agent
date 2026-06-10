using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TravelAgent.Application;
using TravelAgent.Application.Common;
using TravelAgent.Application.Planning;
using TravelAgent.Application.Preferences;
using TravelAgent.Application.Trips;
using TravelAgent.Infrastructure;
using TravelAgent.Infrastructure.Ai;
using TravelAgent.Infrastructure.Persistence;

namespace TravelAgent.UnitTests;

public class DependencyInjectionTests
{
    private static IConfiguration Config(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    private static ServiceProvider BuildProvider(Dictionary<string, string?> config)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddInfrastructure(Config(config));
        return services.BuildServiceProvider();
    }

    [Fact]
    public void Missing_connection_string_fails_fast()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => BuildProvider([]));
        Assert.Contains("TravelAgentDb", ex.Message);
    }

    [Fact]
    public void Without_openai_settings_the_mock_planner_is_used()
    {
        using var provider = BuildProvider(new Dictionary<string, string?>
        {
            ["ConnectionStrings:TravelAgentDb"] = "Server=ignored;Database=ignored",
        });

        Assert.IsType<MockAiPlannerService>(provider.GetRequiredService<IAiPlannerService>());
    }

    [Fact]
    public void With_openai_settings_the_azure_planner_is_used()
    {
        using var provider = BuildProvider(new Dictionary<string, string?>
        {
            ["ConnectionStrings:TravelAgentDb"] = "Server=ignored;Database=ignored",
            ["AzureOpenAI:Endpoint"] = "https://example.openai.azure.com/",
            ["AzureOpenAI:ApiKey"] = "fake-key",
            ["AzureOpenAI:Deployment"] = "planner",
        });

        using var scope = provider.CreateScope();
        Assert.IsType<AzureOpenAiPlannerService>(scope.ServiceProvider.GetRequiredService<IAiPlannerService>());
    }

    [Fact]
    public void Application_and_infrastructure_services_resolve_in_a_scope()
    {
        using var provider = BuildProvider(new Dictionary<string, string?>
        {
            ["ConnectionStrings:TravelAgentDb"] = "Server=ignored;Database=ignored",
        });

        using var scope = provider.CreateScope();
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ITripService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ITripPlanningService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IPreferenceService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ICurrentUserService>());
        // IApplicationDbContext resolves to the same scoped EF context instance.
        Assert.Same(
            scope.ServiceProvider.GetRequiredService<TravelAgentDbContext>(),
            scope.ServiceProvider.GetRequiredService<IApplicationDbContext>());
    }

    [Fact]
    public void Design_time_factory_creates_a_sql_server_context()
    {
        using var context = new DesignTimeDbContextFactory().CreateDbContext([]);
        Assert.Equal("Microsoft.EntityFrameworkCore.SqlServer", context.Database.ProviderName);
    }

    [Theory]
    [InlineData("", "key", "deploy", false)]
    [InlineData("https://e", "", "deploy", false)]
    [InlineData("https://e", "key", "", false)]
    [InlineData("https://e", "key", "deploy", true)]
    public void AzureOpenAiOptions_requires_all_three_settings(string endpoint, string key, string deployment, bool expected)
    {
        var options = new AzureOpenAiOptions { Endpoint = endpoint, ApiKey = key, Deployment = deployment };
        Assert.Equal(expected, options.IsConfigured);
    }
}
