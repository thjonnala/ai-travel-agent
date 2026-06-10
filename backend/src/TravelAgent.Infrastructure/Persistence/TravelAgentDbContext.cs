using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TravelAgent.Application.Common;
using TravelAgent.Domain.Entities;

namespace TravelAgent.Infrastructure.Persistence;

public class TravelAgentDbContext(DbContextOptions<TravelAgentDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<TravelerPreference> TravelerPreferences => Set<TravelerPreference>();
    public DbSet<Trip> Trips => Set<Trip>();
    public DbSet<ItineraryDay> ItineraryDays => Set<ItineraryDay>();
    public DbSet<ItineraryItem> ItineraryItems => Set<ItineraryItem>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TravelAgentDbContext).Assembly);

        // SQLite (used by the test suites) can't compare/order DateTimeOffset;
        // store it as ticks there. SQL Server keeps native datetimeoffset.
        if (Database.ProviderName?.EndsWith("Sqlite", StringComparison.Ordinal) == true)
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties()
                    .Where(p => p.ClrType == typeof(DateTimeOffset) || p.ClrType == typeof(DateTimeOffset?)))
                {
                    property.SetValueConverter(new DateTimeOffsetToBinaryConverter());
                }
            }
        }
    }
}
