using TravelAgent.Application.Planning;
using TravelAgent.Domain;

namespace TravelAgent.UnitTests;

public class ItineraryValidatorTests
{
    private static ItineraryDraft ValidDraft() => new()
    {
        Title = "3 days in Lisbon",
        Destination = "Lisbon, Portugal",
        Currency = "eur",
        Days =
        [
            new ItineraryDayDraft
            {
                DayNumber = 5, // wrong on purpose; should be renumbered to 1
                Summary = "Old town",
                Items =
                [
                    new ItineraryItemDraft
                    {
                        TimeBlock = TimeBlock.Morning,
                        Type = ItineraryItemType.Activity,
                        Title = "Alfama walk",
                        EstimatedCost = -5, // should clamp to 0
                    },
                ],
            },
        ],
    };

    [Fact]
    public void Valid_draft_passes_and_is_normalized()
    {
        var draft = ValidDraft();

        var errors = ItineraryValidator.Validate(draft);

        Assert.Empty(errors);
        Assert.Equal(1, draft.Days[0].DayNumber);
        Assert.Equal(0, draft.Days[0].Items[0].EstimatedCost);
        // Currency is always normalized to USD, whatever the model returned.
        Assert.Equal("USD", draft.Currency);
    }

    [Fact]
    public void Empty_days_fails()
    {
        var draft = ValidDraft();
        draft.Days = [];

        var errors = ItineraryValidator.Validate(draft);

        Assert.Contains(errors, e => e.Contains("at least one day"));
    }

    [Fact]
    public void Day_without_items_fails()
    {
        var draft = ValidDraft();
        draft.Days[0].Items = [];

        var errors = ItineraryValidator.Validate(draft);

        Assert.Contains(errors, e => e.Contains("at least one item"));
    }

    [Fact]
    public void End_date_before_start_date_fails()
    {
        var draft = ValidDraft();
        draft.StartDate = new DateOnly(2026, 10, 10);
        draft.EndDate = new DateOnly(2026, 10, 5);

        var errors = ItineraryValidator.Validate(draft);

        Assert.Contains(errors, e => e.Contains("endDate"));
    }

    [Fact]
    public void Overlong_strings_are_truncated_to_column_limits()
    {
        var draft = ValidDraft();
        draft.Title = new string('x', 500);
        draft.Days[0].Items[0].Description = new string('y', 5000);

        var errors = ItineraryValidator.Validate(draft);

        Assert.Empty(errors);
        Assert.Equal(200, draft.Title.Length);
        Assert.Equal(2000, draft.Days[0].Items[0].Description!.Length);
    }
}
