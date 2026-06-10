using TravelAgent.Domain;

namespace TravelAgent.Application.Planning;

/// <summary>
/// The structured itinerary the AI must produce. This mirrors the strict JSON
/// schema sent to the model (see ItinerarySchema in Infrastructure) and is the
/// only shape the planning pipeline accepts.
/// </summary>
public sealed class ItineraryDraft
{
    public required string Title { get; set; }
    public required string Destination { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }

    /// <summary>ISO 4217 code for every cost estimate in the draft.</summary>
    public string Currency { get; set; } = "USD";

    /// <summary>Short conversational reply shown in the chat panel.</summary>
    public string AssistantMessage { get; set; } = "";

    public List<ItineraryDayDraft> Days { get; set; } = [];
}

public sealed class ItineraryDayDraft
{
    public int DayNumber { get; set; }
    public DateOnly? Date { get; set; }
    public string? Summary { get; set; }
    public List<ItineraryItemDraft> Items { get; set; } = [];
}

public sealed class ItineraryItemDraft
{
    public TimeBlock TimeBlock { get; set; }
    public ItineraryItemType Type { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public decimal EstimatedCost { get; set; }
    public string? LocationName { get; set; }
}
