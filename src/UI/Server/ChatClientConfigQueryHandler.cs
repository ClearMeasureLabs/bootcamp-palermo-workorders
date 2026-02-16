using ClearMeasure.Bootcamp.LlmGateway;
using ClearMeasure.Bootcamp.UI.Client;
using MediatR;

namespace ClearMeasure.Bootcamp.UI.Server;

public class ChatClientConfigQueryHandler(IConfiguration configuration)
    : IRequestHandler<ChatClientConfigQuery, ChatClientConfig>
{
    public Task<ChatClientConfig> Handle(ChatClientConfigQuery request, CancellationToken cancellationToken)
    {
        var connectionString = configuration.GetConnectionString("AzureOpenAI");

        string? apiKey = null;
        string? openAiUrl = null;
        string? openAiModel = null;

        if (!string.IsNullOrEmpty(connectionString))
        {
            var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Split('=', 2))
                .Where(p => p.Length == 2)
                .ToDictionary(p => p[0].Trim(), p => p[1].Trim(), StringComparer.OrdinalIgnoreCase);

            parts.TryGetValue("Key", out apiKey);
            parts.TryGetValue("Endpoint", out openAiUrl);
            parts.TryGetValue("Model", out openAiModel);
        }

        apiKey ??= configuration.GetValue<string>("AI_OpenAI_ApiKey");
        openAiUrl ??= configuration.GetValue<string>("AI_OpenAI_Url");
        openAiModel ??= configuration.GetValue<string>("AI_OpenAI_Model");

        return Task.FromResult(new ChatClientConfig
        {
            AiOpenAiApiKey = apiKey,
            AiOpenAiUrl = openAiUrl,
            AiOpenAiModel = openAiModel
        });
    }
}