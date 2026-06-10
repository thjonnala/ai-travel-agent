namespace TravelAgent.Domain.Entities;

/// <summary>A saved trip: top-level itinerary container owned by one user.</summary>
public class Trip
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public required string Title { get; set; }

    public required string Destination { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public TripStatus Status { get; set; }

    /// <summary>Sum of day estimates, denormalized for cheap list views.</summary>
    public decimal EstimatedTotalCost { get; set; }

    /// <summary>ISO 4217 currency code for all cost estimates on this trip.</summary>
    public string Currency { get; set; } = "USD";

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<ItineraryDay> Days { get; set; } = [];

    public ICollection<ChatMessage> ChatMessages { get; set; } = [];
}
