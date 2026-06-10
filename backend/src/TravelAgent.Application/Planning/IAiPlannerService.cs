using TravelAgent.Domain;

namespace TravelAgent.Application.Planning;

/// <summary>
/// Provider-agnostic AI planning abstraction. Implementations (Azure OpenAI
/// today, possibly Anthropic later) must return a schema-valid
/// <see cref="ItineraryDraft"/> or throw <see cref="AiPlannerException"/>.
/// </summary>
public interface IAiPlannerService
{
    Task<ItineraryDraft> GeneratePlanAsync(PlanningContext context, CancellationToken cancellationToken = default);
}

/// <summary>Everything the model needs: the new request, stored preferences, current plan, and chat history.</summary>
public sealed record PlanningContext(
    string UserRequest,
    TravelerPreferenceInfo? Preferences,
    ItineraryDraft? CurrentItinerary,
    IReadOnlyList<ChatTurn> History);

public sealed record ChatTurn(ChatRole Role, string Content);

/// <summary>Preference snapshot injected into prompts (decoupled from the entity).</summary>
public sealed record TravelerPreferenceInfo(
    BudgetBand BudgetBand,
    TripPace Pace,
    IReadOnlyList<string> Interests,
    string? DietaryNeeds,
    string? Accessibility);

/// <summary>Thrown when the AI provider fails or keeps returning invalid output.</summary>
public sealed class AiPlannerException(string message, Exception? inner = null) : Exception(message, inner);
