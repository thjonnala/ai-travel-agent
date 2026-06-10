using TravelAgent.Application.Planning;
using TravelAgent.Application.Trips;
using TravelAgent.Domain;
using TravelAgent.Domain.Entities;

namespace TravelAgent.UnitTests;

public class TripPlanningServiceTests : IDisposable
{
    private readonly TestDb _db = new();
    private readonly FakePlanner _planner = new();
    private readonly TripPlanningService _service;
    private readonly Guid _userId;

    public TripPlanningServiceTests()
    {
        var user = new User { ExternalAuthId = "test", Email = "t@example.com", DisplayName = "Test" };
        _db.Context.Users.Add(user);
        _db.Context.SaveChanges();
        _userId = user.Id;

        _service = new TripPlanningService(_db.Context, _planner);
    }

    public void Dispose() => _db.Dispose();

    private sealed class FakePlanner : IAiPlannerService
    {
        public PlanningContext? LastContext { get; private set; }
        public ItineraryDraft NextDraft { get; set; } = MakeDraft();

        public Task<ItineraryDraft> GeneratePlanAsync(PlanningContext context, CancellationToken cancellationToken = default)
        {
            LastContext = context;
            return Task.FromResult(NextDraft);
        }
    }

    private static ItineraryDraft MakeDraft(string title = "Lisbon getaway") => new()
    {
        Title = title,
        Destination = "Lisbon, Portugal",
        Currency = "EUR",
        AssistantMessage = "Here you go!",
        Days =
        [
            new ItineraryDayDraft
            {
                DayNumber = 1,
                Summary = "Arrival day",
                Items =
                [
                    new ItineraryItemDraft { TimeBlock = TimeBlock.Morning, Type = ItineraryItemType.Activity, Title = "Alfama walk", EstimatedCost = 10 },
                    new ItineraryItemDraft { TimeBlock = TimeBlock.Evening, Type = ItineraryItemType.Lodging, Title = "Hotel", EstimatedCost = 120 },
                ],
            },
            new ItineraryDayDraft
            {
                DayNumber = 2,
                Summary = "Food day",
                Items =
                [
                    new ItineraryItemDraft { TimeBlock = TimeBlock.Afternoon, Type = ItineraryItemType.Dining, Title = "Time Out Market", EstimatedCost = 25 },
                ],
            },
        ],
    };

    [Fact]
    public async Task Plan_creates_trip_with_server_derived_costs_and_chat_turns()
    {
        var result = await _service.PlanAsync(_userId, new PlanTripRequest { Request = "3 days in Lisbon" });

        Assert.NotNull(result);
        Assert.Equal("Lisbon getaway", result.Title);
        Assert.Equal(2, result.Days.Count);
        // Costs are summed server-side, never trusted from the model.
        Assert.Equal(130, result.Days[0].EstimatedDayCost);
        Assert.Equal(25, result.Days[1].EstimatedDayCost);
        Assert.Equal(155, result.EstimatedTotalCost);
        // One user turn + one assistant turn persisted, in order.
        Assert.Equal(2, result.ChatMessages.Count);
        Assert.Equal(ChatRole.User, result.ChatMessages[0].Role);
        Assert.Equal(ChatRole.Assistant, result.ChatMessages[1].Role);
    }

    [Fact]
    public async Task Refine_replaces_itinerary_and_feeds_history_and_current_plan_to_the_model()
    {
        var created = await _service.PlanAsync(_userId, new PlanTripRequest { Request = "3 days in Lisbon" });
        _planner.NextDraft = MakeDraft("Cheaper Lisbon");

        var refined = await _service.PlanAsync(_userId, new PlanTripRequest { Request = "make day 2 cheaper", TripId = created!.Id });

        Assert.NotNull(refined);
        Assert.Equal(created.Id, refined.Id);
        Assert.Equal("Cheaper Lisbon", refined.Title);
        Assert.Equal(4, refined.ChatMessages.Count);

        // The model saw the existing plan and the prior conversation.
        Assert.NotNull(_planner.LastContext?.CurrentItinerary);
        Assert.Equal(2, _planner.LastContext!.History.Count);
        Assert.Equal("3 days in Lisbon", _planner.LastContext.History[0].Content);
    }

    [Fact]
    public async Task Refining_someone_elses_trip_returns_null()
    {
        var created = await _service.PlanAsync(_userId, new PlanTripRequest { Request = "3 days in Lisbon" });

        var otherUser = new User { ExternalAuthId = "other", Email = "o@example.com", DisplayName = "Other" };
        _db.Context.Users.Add(otherUser);
        await _db.Context.SaveChangesAsync();

        var result = await _service.PlanAsync(otherUser.Id, new PlanTripRequest { Request = "hijack", TripId = created!.Id });

        Assert.Null(result);
    }

    [Fact]
    public async Task Invalid_ai_output_throws_instead_of_persisting()
    {
        _planner.NextDraft = new ItineraryDraft { Title = "bad", Destination = "x", Days = [] };

        await Assert.ThrowsAsync<AiPlannerException>(() =>
            _service.PlanAsync(_userId, new PlanTripRequest { Request = "3 days in Lisbon" }));
        Assert.Empty(_db.Context.Trips);
    }
}
