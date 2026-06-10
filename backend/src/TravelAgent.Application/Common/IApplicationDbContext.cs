using Microsoft.EntityFrameworkCore;
using TravelAgent.Domain.Entities;

namespace TravelAgent.Application.Common;

/// <summary>
/// Persistence abstraction so Application services don't depend on the
/// concrete EF context in Infrastructure. Implemented by TravelAgentDbContext.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<TravelerPreference> TravelerPreferences { get; }
    DbSet<Trip> Trips { get; }
    DbSet<ItineraryDay> ItineraryDays { get; }
    DbSet<ItineraryItem> ItineraryItems { get; }
    DbSet<ChatMessage> ChatMessages { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
