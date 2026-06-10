namespace TravelAgent.Domain.Entities;

/// <summary>A single activity/dining/lodging/transport entry within a day.</summary>
public class ItineraryItem
{
    public Guid Id { get; set; }

    public Guid ItineraryDayId { get; set; }
    public ItineraryDay ItineraryDay { get; set; } = null!;

    public TimeBlock TimeBlock { get; set; }

    public ItineraryItemType Type { get; set; }

    /// <summary>Position within the time block, for stable ordering.</summary>
    public int SortOrder { get; set; }

    public required string Title { get; set; }

    public string? Description { get; set; }

    public decimal EstimatedCost { get; set; }

    public string? LocationName { get; set; }

    public double? Lat { get; set; }

    public double? Lng { get; set; }
}
