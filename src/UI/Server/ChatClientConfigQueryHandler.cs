using ClearMeasure.Bootcamp.LlmGateway;
using ClearMeasure.Bootcamp.UI.Client;
using MediatR;

namespace ClearMeasure.Bootcamp.UI.Server;

public class ChatClientConfigQueryHandler(IConfiguration configuration)
    : IRequestHandler<ChatClientConfigQuery, ChatClientConfig>
{
    public Task<ChatClientConfig> Handle(ChatClientConfigQuery request, CancellationToken cancellationToken)
    {
        var apiKey = configuration.GetValue<string>("AI_OpenAI_ApiKey");
        var openAiUrl = configuration.GetValue<string>("AI_OpenAI_Url");
        var openAiModel = configuration.GetValue<string>("AI_OpenAI_Model");
        return Task.FromResult(new ChatClientConfig
        {
            AiOpenAiApiKey = apiKey, 
            AiOpenAiUrl = openAiUrl, 
            AiOpenAiModel = openAiModel
        });
    }
}