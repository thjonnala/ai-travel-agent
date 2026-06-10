using System.ClientModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;
using TravelAgent.Application.Planning;
using TravelAgent.Domain;

namespace TravelAgent.Infrastructure.Ai;

/// <summary>
/// Azure OpenAI implementation of the planner. Uses structured outputs (strict
/// JSON schema) and re-prompts once with the validation errors if the model
/// still manages to return an invalid itinerary.
/// </summary>
public sealed class AzureOpenAiPlannerService(ChatClient chatClient, ILogger<AzureOpenAiPlannerService> logger) : IAiPlannerService
{
    internal static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    public async Task<ItineraryDraft> GeneratePlanAsync(PlanningContext context, CancellationToken cancellationToken = default)
    {
        var messages = BuildMessages(context);
        var options = new ChatCompletionOptions
        {
            ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                jsonSchemaFormatName: "itinerary",
                jsonSchema: BinaryData.FromString(ItinerarySchema.Json),
                jsonSchemaIsStrict: true),
        };

        var rawResponse = await CompleteAsync(messages, options, cancellationToken);
        var (draft, errors) = TryParse(rawResponse);
        if (draft is not null) return draft;

        // One corrective round-trip: show the model its own output and the errors.
        logger.LogWarning("AI itinerary failed validation, re-prompting once. Errors: {Errors}", string.Join("; ", errors));
        messages.Add(new AssistantChatMessage(rawResponse));
        messages.Add(new UserChatMessage(
            $"That response was invalid: {string.Join("; ", errors)}. Return the corrected itinerary as schema-valid JSON only."));

        rawResponse = await CompleteAsync(messages, options, cancellationToken);
        (draft, errors) = TryParse(rawResponse);
        return draft ?? throw new AiPlannerException($"AI returned an invalid itinerary after retry: {string.Join("; ", errors)}");
    }

    private async Task<string> CompleteAsync(List<ChatMessage> messages, ChatCompletionOptions options, CancellationToken cancellationToken)
    {
        try
        {
            // Transient failures (429/5xx) are retried by the SDK's built-in
            // exponential-backoff pipeline; we only translate terminal errors.
            ChatCompletion completion = await chatClient.CompleteChatAsync(messages, options, cancellationToken);
            return completion.Content[0].Text;
        }
        catch (ClientResultException ex)
        {
            throw new AiPlannerException($"Azure OpenAI request failed (status {ex.Status}).", ex);
        }
    }

    private static List<ChatMessage> BuildMessages(PlanningContext context)
    {
        var messages = new List<ChatMessage> { new SystemChatMessage(ItinerarySchema.BuildSystemPrompt(context)) };

        if (context.CurrentItinerary is { } current)
            messages.Add(new UserChatMessage(ItinerarySchema.BuildCurrentItineraryMessage(current, SerializerOptions)));

        foreach (var turn in context.History)
        {
            // Assistant turns hold the conversational reply only; the full plan
            // travels via the current-itinerary snapshot above, keeping tokens low.
            messages.Add(turn.Role == ChatRole.User
                ? new UserChatMessage(turn.Content)
                : new AssistantChatMessage(turn.Content));
        }

        messages.Add(new UserChatMessage(context.UserRequest));
        return messages;
    }

    private static (ItineraryDraft? Draft, IReadOnlyList<string> Errors) TryParse(string raw)
    {
        ItineraryDraft? draft;
        try
        {
            draft = JsonSerializer.Deserialize<ItineraryDraft>(raw, SerializerOptions);
        }
        catch (JsonException ex)
        {
            return (null, [$"response is not valid JSON for the schema: {ex.Message}"]);
        }

        if (draft is null) return (null, ["response was empty"]);

        var errors = ItineraryValidator.Validate(draft);
        return errors.Count > 0 ? (null, errors) : (draft, []);
    }
}
