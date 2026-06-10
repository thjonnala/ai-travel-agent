using System.Text.RegularExpressions;
using TravelAgent.Application.Planning;
using TravelAgent.Domain;

namespace TravelAgent.Infrastructure.Ai;

/// <summary>
/// Deterministic stand-in used when Azure OpenAI is not configured, so the app
/// demos end-to-end without keys. Produces a plausible itinerary from the
/// request text and stored preferences.
/// </summary>
public sealed partial class MockAiPlannerService : IAiPlannerService
{
    // Allows one adjective between the number and the noun: "5 relaxed days".
    [GeneratedRegex(@"(\d+)(?:\s+\w+)?\s*-?\s*(?:day|night)s?\b", RegexOptions.IgnoreCase)]
    private static partial Regex DayCountPattern();

    // First word matches any casing ("miami"); follow-on words must be
    // capitalized so trailing prose isn't swallowed ("in lisbon in october").
    [GeneratedRegex(@"\b(?i:in|to|around|visit(?:ing)?)\s+([\p{L}]+(?:\s[A-Z][\p{L}]+)*)")]
    private static partial Regex DestinationPattern();

    /// <summary>Words that follow "to"/"in" but are never destinations.</summary>
    private static readonly HashSet<string> NonDestinationWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "go", "going", "travel", "traveling", "travelling", "see", "visit", "spend",
        "stay", "do", "the", "a", "an", "have", "be", "make", "plan", "explore", "relax",
    };

    private static string? ExtractDestination(string request)
    {
        foreach (Match match in DestinationPattern().Matches(request))
        {
            var candidate = match.Groups[1].Value.Trim();
            if (!NonDestinationWords.Contains(candidate.Split(' ')[0]))
                return System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(candidate.ToLowerInvariant());
        }

        return null;
    }

    public Task<ItineraryDraft> GeneratePlanAsync(PlanningContext context, CancellationToken cancellationToken = default)
    {
        var destination = context.CurrentItinerary?.Destination
            ?? ExtractDestination(context.UserRequest)
            ?? "Lisbon";

        var dayCountMatch = DayCountPattern().Match(context.UserRequest);
        var dayCount = dayCountMatch.Success && int.TryParse(dayCountMatch.Groups[1].Value, out var parsed)
            ? Math.Clamp(parsed, 1, 14)
            : context.CurrentItinerary?.Days.Count ?? 3;

        var costFactor = context.Preferences?.BudgetBand switch
        {
            BudgetBand.Budget => 0.6m,
            BudgetBand.Luxury => 2.5m,
            _ => 1m,
        };

        var draft = new ItineraryDraft
        {
            Title = $"{dayCount} days in {destination}",
            Destination = destination,
            StartDate = context.CurrentItinerary?.StartDate,
            EndDate = context.CurrentItinerary?.EndDate,
            Currency = context.CurrentItinerary?.Currency ?? "EUR",
            AssistantMessage =
                $"Here's a {dayCount}-day plan for {destination}. " +
                "(Mock planner: configure AzureOpenAI settings to get real AI itineraries.)",
            Days = [.. Enumerable.Range(1, dayCount).Select(n => BuildDay(n, destination, costFactor))],
        };

        return Task.FromResult(draft);
    }

    private static ItineraryDayDraft BuildDay(int dayNumber, string destination, decimal costFactor) => new()
    {
        DayNumber = dayNumber,
        Summary = $"Day {dayNumber}: exploring {destination} at an easy pace.",
        Items =
        [
            new ItineraryItemDraft
            {
                TimeBlock = TimeBlock.Morning,
                Type = ItineraryItemType.Activity,
                Title = $"Walking tour of {destination}'s old town",
                Description = "Start with the historic center: main squares, viewpoints, and local markets.",
                EstimatedCost = Math.Round(15 * costFactor, 2),
                LocationName = $"{destination} old town",
            },
            new ItineraryItemDraft
            {
                TimeBlock = TimeBlock.Afternoon,
                Type = ItineraryItemType.Dining,
                Title = "Lunch at a neighborhood restaurant",
                Description = "Seasonal local dishes; ask for the daily special.",
                EstimatedCost = Math.Round(20 * costFactor, 2),
                LocationName = "City center",
            },
            new ItineraryItemDraft
            {
                TimeBlock = TimeBlock.Afternoon,
                Type = ItineraryItemType.Activity,
                Title = dayNumber % 2 == 0 ? "Museum or gallery visit" : "Scenic neighborhood stroll",
                Description = "A slower-paced cultural stop matched to your interests.",
                EstimatedCost = Math.Round(12 * costFactor, 2),
                LocationName = destination,
            },
            new ItineraryItemDraft
            {
                TimeBlock = TimeBlock.Evening,
                Type = ItineraryItemType.Dining,
                Title = "Dinner with a view",
                Description = "Relaxed dinner spot; book ahead on weekends.",
                EstimatedCost = Math.Round(35 * costFactor, 2),
                LocationName = destination,
            },
            new ItineraryItemDraft
            {
                TimeBlock = TimeBlock.Evening,
                Type = ItineraryItemType.Lodging,
                Title = "Mid-range hotel near the center",
                Description = "Comfortable base within walking distance of the main sights.",
                EstimatedCost = Math.Round(110 * costFactor, 2),
                LocationName = $"{destination} center",
            },
        ],
    };
}
