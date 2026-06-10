using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TravelAgent.IntegrationTests;

public class TripsApiTests : IClassFixture<TestWebApplicationFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    private readonly HttpClient _client;

    public TripsApiTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<TripDetail> PlanTripAsync(string request)
    {
        var response = await _client.PostAsJsonAsync("/api/trips/plan", new { request });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TripDetail>(Json))!;
    }

    [Fact]
    public async Task Plan_then_get_then_list_then_delete_roundtrip()
    {
        var planned = await PlanTripAsync("4 days in Porto, mid-range budget");

        Assert.Equal(4, planned.Days.Count);
        Assert.True(planned.EstimatedTotalCost > 0);
        Assert.Equal(2, planned.ChatMessages.Count);

        var fetched = await _client.GetFromJsonAsync<TripDetail>($"/api/trips/{planned.Id}", Json);
        Assert.Equal(planned.Id, fetched!.Id);
        Assert.Equal(planned.EstimatedTotalCost, fetched.EstimatedTotalCost);

        var list = await _client.GetFromJsonAsync<PagedResult>("/api/trips", Json);
        Assert.Contains(list!.Items, t => t.Id == planned.Id);

        var delete = await _client.DeleteAsync($"/api/trips/{planned.Id}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        var gone = await _client.GetAsync($"/api/trips/{planned.Id}");
        Assert.Equal(HttpStatusCode.NotFound, gone.StatusCode);
    }

    [Fact]
    public async Task Chat_refinement_appends_to_conversation()
    {
        var planned = await PlanTripAsync("3 days in Madrid");

        var response = await _client.PostAsJsonAsync($"/api/trips/{planned.Id}/chat", new { message = "make day 2 cheaper" });
        response.EnsureSuccessStatusCode();
        var refined = await response.Content.ReadFromJsonAsync<TripDetail>(Json);

        Assert.Equal(planned.Id, refined!.Id);
        Assert.Equal(4, refined.ChatMessages.Count);
    }

    [Fact]
    public async Task Plan_with_unknown_tripId_returns_404()
    {
        var response = await _client.PostAsJsonAsync("/api/trips/plan", new { request = "extend my trip", tripId = Guid.NewGuid() });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Plan_with_too_short_request_returns_400()
    {
        var response = await _client.PostAsJsonAsync("/api/trips/plan", new { request = "ab" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Duplicate_creates_a_copy_without_chat_history()
    {
        var planned = await PlanTripAsync("2 days in Seville");

        var response = await _client.PostAsync($"/api/trips/{planned.Id}/duplicate", null);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var copy = await response.Content.ReadFromJsonAsync<TripDetail>(Json);

        Assert.NotEqual(planned.Id, copy!.Id);
        Assert.StartsWith(planned.Title, copy.Title);
        Assert.Equal(planned.Days.Count, copy.Days.Count);
        Assert.Empty(copy.ChatMessages);
    }

    [Fact]
    public async Task Preferences_roundtrip_and_feed_into_planning()
    {
        var put = await _client.PutAsJsonAsync("/api/preferences", new
        {
            budgetBand = "luxury",
            pace = "relaxed",
            interests = new[] { "food", "history" },
            dietaryNeeds = "vegetarian",
        });
        put.EnsureSuccessStatusCode();

        var preferences = await _client.GetFromJsonAsync<JsonElement>("/api/preferences");
        Assert.Equal("luxury", preferences.GetProperty("budgetBand").GetString());

        // The mock planner scales costs by budget band, proving preferences flow into planning.
        var luxuryTrip = await PlanTripAsync("2 days in Lisbon");
        Assert.True(luxuryTrip.EstimatedTotalCost > 300);
    }

    // Minimal response shapes — only what the assertions need.
    private sealed record TripDetail(Guid Id, string Title, decimal EstimatedTotalCost,
        List<Day> Days, List<Message> ChatMessages);
    private sealed record Day(Guid Id, int DayNumber, decimal EstimatedDayCost, List<Item> Items);
    private sealed record Item(Guid Id, string Title, decimal EstimatedCost);
    private sealed record Message(Guid Id, string Role, string Content);
    private sealed record PagedResult(List<TripSummary> Items, int TotalCount);
    private sealed record TripSummary(Guid Id, string Title);
}
