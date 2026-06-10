using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TravelAgent.Infrastructure.Persistence;

/// <summary>
/// Lets `dotnet ef` and migration bundles create the context without the API
/// host's configuration. No connection string here: the tooling supplies it
/// (e.g. the bundle's --connection flag in the deploy pipeline).
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<TravelAgentDbContext>
{
    public TravelAgentDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<TravelAgentDbContext>()
            .UseSqlServer()
            .Options;
        return new TravelAgentDbContext(options);
    }
}
