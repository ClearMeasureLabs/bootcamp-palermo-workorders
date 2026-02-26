using Azure;
using Azure.AI.OpenAI;
using ClearMeasure.Bootcamp.Core;
using Microsoft.Extensions.AI;
using OpenAI.Chat;

namespace ClearMeasure.Bootcamp.LlmGateway;

public class ChatClientFactory(IBus bus)
{
    public async Task<IChatClient> GetChatClient()
    {
        var config = await bus.Send(new ChatClientConfigQuery());
        var apiKey = config.AiOpenAiApiKey
            ?? throw new InvalidOperationException("AI_OpenAI_ApiKey is not configured.");

        IChatClient innerClient = BuildAzureOpenAiChatClient(config, apiKey);

        return new TracingChatClient(innerClient, config);
    }

    private static IChatClient BuildAzureOpenAiChatClient(ChatClientConfig config, string apiKey)
    {
        var openAiUrl = config.AiOpenAiUrl;
        var openAiModel = config.AiOpenAiModel;

        var credential = new AzureKeyCredential(apiKey ?? throw new InvalidOperationException());
        var uri = new Uri(openAiUrl ?? throw new InvalidOperationException());
        var openAiClient = new AzureOpenAIClient(uri, credential);

        ChatClient chatClient = openAiClient.GetChatClient(openAiModel);
        return chatClient.AsIChatClient()
            .AsBuilder()
            .UseFunctionInvocation()
            .Build();
    }
}