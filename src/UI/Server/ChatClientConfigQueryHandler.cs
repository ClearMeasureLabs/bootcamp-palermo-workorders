using ClearMeasure.Bootcamp.LlmGateway;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;

namespace ClearMeasure.Bootcamp.UI.Server;

public class ChatClientConfigQueryHandler(IConfiguration configuration, ILogger<ChatClientConfigQueryHandler> logger)
    : IRequestHandler<ChatClientConfigQuery, ChatClientConfig>
{
    public Task<ChatClientConfig> Handle(ChatClientConfigQuery request, CancellationToken cancellationToken)
    {
        var apiKey = configuration.GetValue<string>("AI_OpenAI_ApiKey");
        var openAiUrl = configuration.GetValue<string>("AI_OpenAI_Url");
        var openAiModel = configuration.GetValue<string>("AI_OpenAI_Model");
        var provider = configuration.GetValue<string>("AI_OpenAI_Provider");
        logger.LogDebug("AI_OpenAI_Provider: {Provider}, AI_OpenAI_Url configured: {HasUrl}, AI_OpenAI_Model configured: {HasModel}, AI_OpenAI_ApiKey configured: {HasKey}",
            string.IsNullOrEmpty(provider) ? "(default Azure)" : provider,
            !string.IsNullOrEmpty(openAiUrl),
            !string.IsNullOrEmpty(openAiModel),
            !string.IsNullOrEmpty(apiKey));

        return Task.FromResult(new ChatClientConfig
        {
            AiOpenAiProvider = provider,
            AiOpenAiApiKey = apiKey,
            AiOpenAiUrl = openAiUrl,
            AiOpenAiModel = openAiModel
        });
    }
}