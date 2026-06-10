namespace TravelAgent.Domain;

/// <summary>Traveler's overall spending appetite, used to steer AI suggestions.</summary>
public enum BudgetBand
{
    Budget,
    MidRange,
    Luxury,
}

/// <summary>How packed the traveler wants each day to be.</summary>
public enum TripPace
{
    Relaxed,
    Moderate,
    Packed,
}

/// <summary>Lifecycle state of a trip.</summary>
public enum TripStatus
{
    Draft,
    Planned,
    Archived,
}

/// <summary>Part of the day an itinerary item belongs to.</summary>
public enum TimeBlock
{
    Morning,
    Afternoon,
    Evening,
}

/// <summary>Kind of itinerary item.</summary>
public enum ItineraryItemType
{
    Activity,
    Dining,
    Lodging,
    Transport,
}

/// <summary>Author of a chat message in the planning conversation.</summary>
public enum ChatRole
{
    User,
    Assistant,
    System,
}
