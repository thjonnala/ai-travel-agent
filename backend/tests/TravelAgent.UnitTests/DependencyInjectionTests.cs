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
    private const string FakeConnString = "Host=localhost;Database=ignored;Username=u;Password=p";

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
    public void Without_ai_settings_the_mock_planner_is_used()
    {
        using var provider = BuildProvider(new Dictionary<string, string?>
        {
            ["ConnectionStrings:TravelAgentDb"] = FakeConnString,
        });

        Assert.IsType<MockAiPlannerService>(provider.GetRequiredService<IAiPlannerService>());
    }

    [Fact]
    public void With_ai_settings_the_openai_compatible_planner_is_used()
    {
        using var provider = BuildProvider(new Dictionary<string, string?>
        {
            ["ConnectionStrings:TravelAgentDb"] = FakeConnString,
            ["Ai:Endpoint"] = "https://api.groq.com/openai/v1",
            ["Ai:ApiKey"] = "fake-key",
            ["Ai:Model"] = "llama-3.3-70b-versatile",
        });

        using var scope = provider.CreateScope();
        Assert.IsType<OpenAiCompatiblePlannerService>(scope.ServiceProvider.GetRequiredService<IAiPlannerService>());
    }

    [Fact]
    public void Application_and_infrastructure_services_resolve_in_a_scope()
    {
        using var provider = BuildProvider(new Dictionary<string, string?>
        {
            ["ConnectionStrings:TravelAgentDb"] = FakeConnString,
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
    public void Design_time_factory_creates_a_postgres_context()
    {
        using var context = new DesignTimeDbContextFactory().CreateDbContext([]);
        Assert.Equal("Npgsql.EntityFrameworkCore.PostgreSQL", context.Database.ProviderName);
    }

    [Theory]
    [InlineData("", "key", "model", false)]
    [InlineData("https://e", "", "model", false)]
    [InlineData("https://e", "key", "", false)]
    [InlineData("https://e", "key", "model", true)]
    public void AiOptions_requires_all_three_settings(string endpoint, string key, string model, bool expected)
    {
        var options = new AiOptions { Endpoint = endpoint, ApiKey = key, Model = model };
        Assert.Equal(expected, options.IsConfigured);
    }
}
