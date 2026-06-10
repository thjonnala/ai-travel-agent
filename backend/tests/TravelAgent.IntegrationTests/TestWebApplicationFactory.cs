using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using TravelAgent.Infrastructure.Persistence;

namespace TravelAgent.IntegrationTests;

/// <summary>
/// Boots the real API with the SQL Server context swapped for SQLite
/// in-memory. No AzureOpenAI config is supplied, so the mock planner is used —
/// the whole pipeline (controller → services → EF) runs for real.
/// </summary>
public sealed class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection.Open();

        builder.UseEnvironment("Development");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(DbContextOptions<TravelAgentDbContext>));
            // EF 9+ also stashes provider config here; without removing it the
            // SqlServer provider stays registered alongside Sqlite.
            services.RemoveAll(typeof(IDbContextOptionsConfiguration<TravelAgentDbContext>));
            services.AddDbContext<TravelAgentDbContext>(options => options.UseSqlite(_connection));

            using var scope = services.BuildServiceProvider().CreateScope();
            scope.ServiceProvider.GetRequiredService<TravelAgentDbContext>().Database.EnsureCreated();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _connection.Dispose();
    }
}

internal static class ServiceCollectionExtensions
{
    public static void RemoveAll(this IServiceCollection services, Type serviceType)
    {
        for (var i = services.Count - 1; i >= 0; i--)
        {
            if (services[i].ServiceType == serviceType) services.RemoveAt(i);
        }
    }
}
