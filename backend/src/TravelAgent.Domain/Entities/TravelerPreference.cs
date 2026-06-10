namespace TravelAgent.Domain.Entities;

/// <summary>
/// Stored traveler preferences, injected automatically into AI planning prompts.
/// One row per user.
/// </summary>
public class TravelerPreference
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public BudgetBand BudgetBand { get; set; }

    public TripPace Pace { get; set; }

    /// <summary>JSON array of interest tags, e.g. ["food","history"].</summary>
    public string Interests { get; set; } = "[]";

    public string? DietaryNeeds { get; set; }

    public string? Accessibility { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
