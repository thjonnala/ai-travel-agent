namespace TravelAgent.Infrastructure.Ai;

public sealed class AzureOpenAiOptions
{
    public const string SectionName = "AzureOpenAI";

    public string Endpoint { get; set; } = "";
    public string ApiKey { get; set; } = "";

    /// <summary>Deployment name of the chat model, e.g. "gpt-4o".</summary>
    public string Deployment { get; set; } = "";

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Endpoint)
        && !string.IsNullOrWhiteSpace(ApiKey)
        && !string.IsNullOrWhiteSpace(Deployment);
}
