namespace TravelAgent.Application.Planning;

/// <summary>
/// Server-side validation/normalization of AI output. Hard failures return
/// errors (used to re-prompt the model once); soft issues are normalized in
/// place so column limits and invariants always hold before persisting.
/// </summary>
public static class ItineraryValidator
{
    public static IReadOnlyList<string> Validate(ItineraryDraft draft)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(draft.Title)) errors.Add("title must not be empty");
        if (string.IsNullOrWhiteSpace(draft.Destination)) errors.Add("destination must not be empty");
        if (draft.Days.Count == 0) errors.Add("days must contain at least one day");
        if (draft.StartDate is { } s && draft.EndDate is { } e && e < s)
            errors.Add("endDate must not be before startDate");

        for (var d = 0; d < draft.Days.Count; d++)
        {
            var day = draft.Days[d];
            if (day.Items.Count == 0) errors.Add($"days[{d}] must contain at least one item");
            foreach (var (item, i) in day.Items.Select((item, i) => (item, i)))
            {
                if (string.IsNullOrWhiteSpace(item.Title))
                    errors.Add($"days[{d}].items[{i}].title must not be empty");
            }
        }

        if (errors.Count > 0) return errors;

        Normalize(draft);
        return errors;
    }

    private static void Normalize(ItineraryDraft draft)
    {
        draft.Title = Truncate(draft.Title.Trim(), 200)!;
        draft.Destination = Truncate(draft.Destination.Trim(), 200)!;
        // Product decision: all estimates are presented in US dollars,
        // regardless of what the model returns.
        draft.Currency = "USD";

        // Re-sequence day numbers so they are always 1..n regardless of model output.
        var dayNumber = 1;
        foreach (var day in draft.Days)
        {
            day.DayNumber = dayNumber++;
            day.Summary = Truncate(day.Summary, 1000);

            var sortOrder = 0;
            foreach (var item in day.Items)
            {
                _ = sortOrder++;
                item.Title = Truncate(item.Title.Trim(), 300)!;
                item.Description = Truncate(item.Description, 2000);
                item.LocationName = Truncate(item.LocationName, 300);
                if (item.EstimatedCost < 0) item.EstimatedCost = 0;
                item.EstimatedCost = Math.Round(item.EstimatedCost, 2);
            }
        }
    }

    private static string? Truncate(string? value, int max) =>
        value is null ? null : value.Length <= max ? value : value[..max];
}
