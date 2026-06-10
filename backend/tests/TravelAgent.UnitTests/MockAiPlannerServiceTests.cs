using TravelAgent.Application.Planning;
using TravelAgent.Domain;
using TravelAgent.Infrastructure.Ai;

namespace TravelAgent.UnitTests;

public class MockAiPlannerServiceTests
{
    private readonly MockAiPlannerService _planner = new();

    [Fact]
    public async Task Extracts_day_count_and_destination_from_request()
    {
        var draft = await _planner.GeneratePlanAsync(
            new PlanningContext("5 relaxed days in Porto, into food", null, null, []));

        Assert.Equal(5, draft.Days.Count);
        Assert.Equal("Porto", draft.Destination);
        Assert.Empty(ItineraryValidator.Validate(draft));
    }

    [Theory]
    [InlineData("nashville to miami 3 days trip july 1st to 4th", "Miami", 3)]
    [InlineData("I want to go to paris for 2 nights", "Paris", 2)]
    [InlineData("plan something fun", "Lisbon", 3)] // no destination → default
    public async Task Handles_lowercase_and_messy_requests(string request, string destination, int days)
    {
        var draft = await _planner.GeneratePlanAsync(new PlanningContext(request, null, null, []));

        Assert.Equal(destination, draft.Destination);
        Assert.Equal(days, draft.Days.Count);
    }

    [Fact]
    public async Task Budget_preference_scales_costs()
    {
        var context = new PlanningContext("3 days in Rome", null, null, []);
        var luxuryContext = context with
        {
            Preferences = new TravelerPreferenceInfo(BudgetBand.Luxury, TripPace.Relaxed, [], null, null),
        };

        var standard = await _planner.GeneratePlanAsync(context);
        var luxury = await _planner.GeneratePlanAsync(luxuryContext);

        decimal Total(ItineraryDraft d) => d.Days.SelectMany(day => day.Items).Sum(i => i.EstimatedCost);
        Assert.True(Total(luxury) > Total(standard));
    }

    [Fact]
    public async Task Refinement_keeps_existing_destination()
    {
        var current = new ItineraryDraft
        {
            Title = "Trip",
            Destination = "Kyoto, Japan",
            Days = [new ItineraryDayDraft { DayNumber = 1, Items = [new ItineraryItemDraft { Title = "x", TimeBlock = TimeBlock.Morning, Type = ItineraryItemType.Activity }] }],
        };

        var draft = await _planner.GeneratePlanAsync(
            new PlanningContext("make it cheaper", null, current, []));

        Assert.Equal("Kyoto, Japan", draft.Destination);
    }
}
