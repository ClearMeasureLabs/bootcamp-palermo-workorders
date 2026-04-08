namespace ClearMeasure.Bootcamp.LlmGateway;

public class ChatClientConfig
{
    /// <summary>
    /// Optional provider mode: null, empty, or <c>AzureOpenAI</c> (default) uses Azure OpenAI;
    /// <c>OpenAICompatible</c> uses an OpenAI-compatible HTTP API (same API key, base URL, and model settings).
    /// </summary>
    public string? AiOpenAiProvider { get; set; }

    public required string? AiOpenAiApiKey { get; set; }
    public required string? AiOpenAiUrl { get; set; }
    public required string? AiOpenAiModel { get; set; }
}