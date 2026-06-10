using System.Text;
using System.Text.Json;
using TravelAgent.Application.Planning;

namespace TravelAgent.Infrastructure.Ai;

/// <summary>
/// The strict JSON schema for structured outputs, plus prompt assembly shared
/// by AI planner implementations. Must stay in sync with
/// <see cref="ItineraryDraft"/>.
/// </summary>
internal static class ItinerarySchema
{
    // OpenAI strict mode requires every property listed in `required` and
    // additionalProperties=false at every level; optionality is expressed
    // with ["type","null"] unions.
    public const string Json = """
        {
          "type": "object",
          "additionalProperties": false,
          "required": ["title", "destination", "startDate", "endDate", "currency", "assistantMessage", "days"],
          "properties": {
            "title": { "type": "string", "description": "Short trip title, e.g. '5 relaxed days in Lisbon'" },
            "destination": { "type": "string", "description": "Primary destination, e.g. 'Lisbon, Portugal'" },
            "startDate": { "type": ["string", "null"], "description": "ISO date yyyy-MM-dd, null if the user gave no dates" },
            "endDate": { "type": ["string", "null"], "description": "ISO date yyyy-MM-dd, null if the user gave no dates" },
            "currency": { "type": "string", "enum": ["USD"], "description": "All cost estimates are expressed in US dollars" },
            "assistantMessage": { "type": "string", "description": "Short friendly reply to the user summarizing what you planned or changed" },
            "days": {
              "type": "array",
              "items": {
                "type": "object",
                "additionalProperties": false,
                "required": ["dayNumber", "date", "summary", "items"],
                "properties": {
                  "dayNumber": { "type": "integer", "description": "1-based day index" },
                  "date": { "type": ["string", "null"], "description": "ISO date yyyy-MM-dd" },
                  "summary": { "type": "string", "description": "One-sentence theme of the day" },
                  "items": {
                    "type": "array",
                    "items": {
                      "type": "object",
                      "additionalProperties": false,
                      "required": ["timeBlock", "type", "title", "description", "estimatedCost", "locationName"],
                      "properties": {
                        "timeBlock": { "type": "string", "enum": ["morning", "afternoon", "evening"] },
                        "type": { "type": "string", "enum": ["activity", "dining", "lodging", "transport"] },
                        "title": { "type": "string" },
                        "description": { "type": "string", "description": "1-3 sentences: what it is and why it fits this traveler" },
                        "estimatedCost": { "type": "number", "description": "Estimated cost per person in the itinerary currency, 0 if free" },
                        "locationName": { "type": ["string", "null"], "description": "Neighborhood or venue name" }
                      }
                    }
                  }
                }
              }
            }
          }
        }
        """;

    public static string BuildSystemPrompt(PlanningContext context)
    {
        var prompt = new StringBuilder(
            """
            You are a knowledgeable, practical travel planner. You design realistic, personalized
            itineraries with concrete suggestions (real neighborhoods, landmarks, dish types) and
            honest per-person cost estimates.

            Rules:
            - Respond ONLY with JSON matching the provided schema. No prose outside the JSON.
            - Write everything in English, and express every cost estimate in US dollars (currency
              "USD"), converting typical local prices to USD.
            - Structure each day into morning/afternoon/evening items. Include lodging once per day
              (type "lodging") and at least one dining suggestion per day.
            - Keep daily pacing consistent with the traveler's preferences below.
            - When refining an existing itinerary, change only what the user asked for and preserve
              everything else, including dates and day numbering.
            - Costs are estimates per person in USD; use 0 for free items. Never invent exact
              prices for specific venues - give realistic ranges rounded to sensible numbers.
            - assistantMessage should be 1-3 friendly sentences describing what you planned or changed.

            """);

        prompt.AppendLine($"Today's date: {DateOnly.FromDateTime(DateTime.UtcNow):yyyy-MM-dd} (resolve relative dates like \"in October\" against this).");

        if (context.Preferences is { } p)
        {
            prompt.AppendLine("Traveler preferences (apply automatically):");
            prompt.AppendLine($"- Budget: {p.BudgetBand}, pace: {p.Pace}");
            if (p.Interests.Count > 0) prompt.AppendLine($"- Interests: {string.Join(", ", p.Interests)}");
            if (!string.IsNullOrWhiteSpace(p.DietaryNeeds)) prompt.AppendLine($"- Dietary needs: {p.DietaryNeeds}");
            if (!string.IsNullOrWhiteSpace(p.Accessibility)) prompt.AppendLine($"- Accessibility: {p.Accessibility}");
        }

        return prompt.ToString();
    }

    public static string BuildCurrentItineraryMessage(ItineraryDraft current, JsonSerializerOptions serializerOptions) =>
        $"Current itinerary (refine this, preserving anything the user didn't ask to change):\n{JsonSerializer.Serialize(current, serializerOptions)}";
}
