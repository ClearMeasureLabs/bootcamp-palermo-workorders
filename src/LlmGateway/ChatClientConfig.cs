namespace ClearMeasure.Bootcamp.LlmGateway;

public class ChatClientConfig
{
    public required string? AiOpenAiApiKey { get; init; }
    public required string? AiOpenAiUrl { get; init; }
    public required string? AiOpenAiModel { get; init; }
}