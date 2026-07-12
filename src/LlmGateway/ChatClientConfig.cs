namespace ClearMeasure.Bootcamp.LlmGateway;

public class ChatClientConfig
{
    public required string? AiOpenAiApiKey { get; set; }
    public required string? AiOpenAiUrl { get; set; }
    public required string? AiOpenAiModel { get; set; }

    /// <summary>
    /// When true, a deterministic offline <see cref="FakeChatClient"/> is used instead of
    /// Azure OpenAI. Enabled via the <c>AI_OpenAI_UseFake</c> configuration key so tests can
    /// exercise the text-only LLM paths with no live external calls.
    /// </summary>
    public bool UseFake { get; set; }
}