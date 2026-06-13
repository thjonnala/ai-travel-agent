using System.ClientModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;
using TravelAgent.Application.Planning;
using TravelAgent.Domain;

namespace TravelAgent.Infrastructure.Ai;

/// <summary>
/// Planner backed by any OpenAI-compatible chat API (Groq, OpenRouter, a
/// self-hosted Ollama, …). Asks for JSON-object responses and relies on the
/// embedded schema in the prompt plus server-side validation — re-prompting
/// once with the validation errors if the model returns invalid output.
/// </summary>
public sealed class OpenAiCompatiblePlannerService(ChatClient chatClient, ILogger<OpenAiCompatiblePlannerService> logger) : IAiPlannerService
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
        // JSON-object mode is supported across providers (Groq/OpenRouter/Ollama);
        // the exact shape is enforced by the schema in the prompt + ItineraryValidator.
        var options = new ChatCompletionOptions { ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat() };

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
            throw new AiPlannerException($"AI provider request failed (status {ex.Status}).", ex);
        }
    }

    private static List<ChatMessage> BuildMessages(PlanningContext context)
    {
        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(ItinerarySchema.BuildSystemPrompt(context)),
            // With JSON-object mode the provider doesn't enforce a schema, so the
            // exact JSON shape is described here for the model to follow.
            new SystemChatMessage($"Return a single JSON object that conforms exactly to this JSON schema:\n{ItinerarySchema.Json}"),
        };

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
