namespace ClearMeasure.Bootcamp.LlmGateway;

internal static class ChatClientConfigValidator
{
    public static ChatClientAvailabilityResult Validate(ChatClientConfig config)
    {
        var missing = new List<string>();

        if (string.IsNullOrEmpty(config.AiOpenAiApiKey)) missing.Add("AI_OpenAI_ApiKey");
        if (string.IsNullOrEmpty(config.AiOpenAiUrl)) missing.Add("AI_OpenAI_Url");
        if (string.IsNullOrEmpty(config.AiOpenAiModel)) missing.Add("AI_OpenAI_Model");

        if (missing.Count > 0)
        {
            return new ChatClientAvailabilityResult(false,
                $"Chat client is not configured. Set the following environment variables: {string.Join(", ", missing)}");
        }

        return new ChatClientAvailabilityResult(true, "Chat client is configured");
    }
}
