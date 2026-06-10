namespace TravelAgent.Domain.Entities;

/// <summary>One day of a trip's itinerary.</summary>
public class ItineraryDay
{
    public Guid Id { get; set; }

    public Guid TripId { get; set; }
    public Trip Trip { get; set; } = null!;

    /// <summary>1-based position within the trip.</summary>
    public int DayNumber { get; set; }

    public DateOnly? Date { get; set; }

    public string? Summary { get; set; }

    public decimal EstimatedDayCost { get; set; }

    public ICollection<ItineraryItem> Items { get; set; } = [];
}
