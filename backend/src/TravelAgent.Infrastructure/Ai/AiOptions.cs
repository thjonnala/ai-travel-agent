namespace TravelAgent.Infrastructure.Ai;

/// <summary>
/// Configuration for the OpenAI-compatible chat provider. Works with any
/// provider that exposes the OpenAI chat-completions API (Groq, OpenRouter,
/// a self-hosted Ollama, etc.) — set the endpoint and model accordingly.
/// </summary>
public sealed class AiOptions
{
    public const string SectionName = "Ai";

    /// <summary>OpenAI-compatible base URL, e.g. "https://api.groq.com/openai/v1".</summary>
    public string Endpoint { get; set; } = "";

    public string ApiKey { get; set; } = "";

    /// <summary>Model id served by the provider, e.g. "llama-3.3-70b-versatile".</summary>
    public string Model { get; set; } = "";

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Endpoint)
        && !string.IsNullOrWhiteSpace(ApiKey)
        && !string.IsNullOrWhiteSpace(Model);
}
