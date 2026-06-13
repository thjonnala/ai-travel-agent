using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TravelAgent.Infrastructure.Persistence;

/// <summary>
/// Lets `dotnet ef` create the context at design time without the API host's
/// configuration. A throwaway connection string satisfies Npgsql's design-time
/// requirement; the real one is supplied at runtime from configuration.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<TravelAgentDbContext>
{
    public TravelAgentDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<TravelAgentDbContext>()
            .UseNpgsql("Host=localhost;Database=travelagent_design_time")
            .Options;
        return new TravelAgentDbContext(options);
    }
}
