using System.ClientModel;
using System.ClientModel.Primitives;
using Microsoft.Extensions.Logging.Abstractions;
using OpenAI.Chat;
using TravelAgent.Application.Planning;
using TravelAgent.Domain;
using TravelAgent.Infrastructure.Ai;

namespace TravelAgent.UnitTests;

public class AzureOpenAiPlannerServiceTests
{
    private const string ValidItineraryJson = """
        {
          "title": "2 days in Porto",
          "destination": "Porto, Portugal",
          "startDate": "2026-10-01",
          "endDate": "2026-10-02",
          "currency": "USD",
          "assistantMessage": "Here you go!",
          "days": [
            {
              "dayNumber": 1,
              "date": "2026-10-01",
              "summary": "Ribeira",
              "items": [
                { "timeBlock": "morning", "type": "activity", "title": "Ribeira walk", "description": "Riverside stroll", "estimatedCost": 0, "locationName": "Ribeira" }
              ]
            }
          ]
        }
        """;

    private static PlanningContext Context(
        TravelerPreferenceInfo? preferences = null,
        ItineraryDraft? current = null,
        IReadOnlyList<ChatTurn>? history = null) =>
        new("2 days in Porto", preferences, current, history ?? []);

    private static AzureOpenAiPlannerService Service(FakeChatClient client) =>
        new(client, NullLogger<AzureOpenAiPlannerService>.Instance);

    [Fact]
    public async Task Returns_parsed_itinerary_on_valid_response()
    {
        var client = new FakeChatClient(ValidItineraryJson);

        var draft = await Service(client).GeneratePlanAsync(Context());

        Assert.Equal("2 days in Porto", draft.Title);
        Assert.Equal("USD", draft.Currency);
        Assert.Single(draft.Days);
        Assert.Equal(TimeBlock.Morning, draft.Days[0].Items[0].TimeBlock);
        Assert.Single(client.Calls);
    }

    [Fact]
    public async Task Prompt_includes_preferences_history_and_current_itinerary()
    {
        var client = new FakeChatClient(ValidItineraryJson);
        var preferences = new TravelerPreferenceInfo(BudgetBand.Luxury, TripPace.Relaxed, ["food"], "vegetarian", "step-free");
        var current = new ItineraryDraft { Title = "Old plan", Destination = "Porto" };
        var history = new[] { new ChatTurn(ChatRole.User, "first ask"), new ChatTurn(ChatRole.Assistant, "first answer") };

        await Service(client).GeneratePlanAsync(Context(preferences, current, history));

        var messages = client.Calls.Single();
        // system + current itinerary + 2 history turns + new request
        Assert.Equal(5, messages.Count);
        var system = messages[0].Content[0].Text;
        Assert.Contains("Luxury", system);
        Assert.Contains("vegetarian", system);
        Assert.Contains("step-free", system);
        Assert.Contains("Old plan", messages[1].Content[0].Text);
        Assert.Equal("first ask", messages[2].Content[0].Text);
        Assert.Equal("2 days in Porto", messages[4].Content[0].Text);
    }

    [Fact]
    public async Task Reprompts_once_with_validation_errors_on_invalid_response()
    {
        var client = new FakeChatClient("this is not json", ValidItineraryJson);

        var draft = await Service(client).GeneratePlanAsync(Context());

        Assert.Equal("2 days in Porto", draft.Title);
        Assert.Equal(2, client.Calls.Count);
        // The corrective turn shows the model its own output and the errors.
        var secondCall = client.Calls[1];
        Assert.Contains(secondCall, m => m.Content[0].Text.Contains("invalid"));
    }

    [Fact]
    public async Task Throws_when_response_is_still_invalid_after_retry()
    {
        var client = new FakeChatClient("garbage", "still garbage");

        await Assert.ThrowsAsync<AiPlannerException>(() => Service(client).GeneratePlanAsync(Context()));
        Assert.Equal(2, client.Calls.Count);
    }

    [Fact]
    public async Task Wraps_provider_errors_in_AiPlannerException()
    {
        var client = new FakeChatClient(ValidItineraryJson)
        {
            ThrowOnCall = new ClientResultException(new FakePipelineResponse(429)),
        };

        var ex = await Assert.ThrowsAsync<AiPlannerException>(() => Service(client).GeneratePlanAsync(Context()));
        Assert.Contains("429", ex.Message);
    }

    // ---- fakes ----

    public sealed class FakeChatClient : ChatClient
    {
        private readonly Queue<string> _responses;

        public List<IReadOnlyList<ChatMessage>> Calls { get; } = [];
        public Exception? ThrowOnCall { get; set; }

        public FakeChatClient(params string[] responses) => _responses = new(responses);

        public override Task<ClientResult<ChatCompletion>> CompleteChatAsync(
            IEnumerable<ChatMessage> messages, ChatCompletionOptions? options = null, CancellationToken cancellationToken = default)
        {
            Calls.Add([.. messages]);
            if (ThrowOnCall is not null) throw ThrowOnCall;

            var completion = OpenAIChatModelFactory.ChatCompletion(
                role: ChatMessageRole.Assistant,
                content: new ChatMessageContent(_responses.Dequeue()));
            return Task.FromResult(ClientResult.FromValue(completion, new FakePipelineResponse(200)));
        }
    }

    public sealed class FakePipelineResponse(int status) : PipelineResponse
    {
        public override int Status => status;
        public override string ReasonPhrase => "";
        public override Stream? ContentStream { get; set; }
        public override BinaryData Content => BinaryData.FromString("");
        protected override PipelineResponseHeaders HeadersCore => new FakeHeaders();
        public override BinaryData BufferContent(CancellationToken cancellationToken = default) => Content;
        public override ValueTask<BinaryData> BufferContentAsync(CancellationToken cancellationToken = default) => new(Content);
        public override void Dispose() { }

        private sealed class FakeHeaders : PipelineResponseHeaders
        {
            public override IEnumerator<KeyValuePair<string, string>> GetEnumerator() =>
                Enumerable.Empty<KeyValuePair<string, string>>().GetEnumerator();
            public override bool TryGetValue(string name, out string? value) { value = null; return false; }
            public override bool TryGetValues(string name, out IEnumerable<string>? values) { values = null; return false; }
        }
    }
}
