using ClearMeasure.Bootcamp.LlmGateway;
using ClearMeasure.Bootcamp.UI.Client;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;

namespace ClearMeasure.Bootcamp.UI.Server;

public class ChatClientConfigQueryHandler(IConfiguration configuration, ILogger<ChatClientConfigQueryHandler> logger)
    : IRequestHandler<ChatClientConfigQuery, ChatClientConfig>
{
    public Task<ChatClientConfig> Handle(ChatClientConfigQuery request, CancellationToken cancellationToken)
    {
        var apiKey = configuration.GetValue<string>("AI_OpenAI_ApiKey");
        logger?.LogDebug($"AI_OpenAI_ApiKey found as {apiKey}");
        var openAiUrl = configuration.GetValue<string>("AI_OpenAI_Url");
        logger?.LogDebug($"AI_OpenAI_Url found as {apiKey}");
        var openAiModel = configuration.GetValue<string>("AI_OpenAI_Model");
        logger?.LogDebug($"AI_OpenAI_Model found as {apiKey}");

        return Task.FromResult(new ChatClientConfig
        {
            AiOpenAiApiKey = apiKey, 
            AiOpenAiUrl = openAiUrl, 
            AiOpenAiModel = openAiModel
        });
    }
}