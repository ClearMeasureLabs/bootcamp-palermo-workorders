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
        var apiKey = config.AiOpenAiApiKey;
        if (string.IsNullOrEmpty(apiKey)) 
            return BuildOllamaChatClient();
        else
        {
            var openAiUrl = config.AiOpenAiUrl;
            var openAiModel = config.AiOpenAiModel;

            var credential = new AzureKeyCredential(apiKey ?? throw new InvalidOperationException());
            var uri = new Uri(openAiUrl ?? throw new InvalidOperationException());
            var openAiClient = new AzureOpenAIClient(uri, credential);

            ChatClient chatClient = openAiClient.GetChatClient(openAiModel);
            return chatClient.AsIChatClient();
        }
    }

    private static IChatClient BuildOllamaChatClient()
    {
        var endpoint = "http://localhost:11434/";
        var modelId = "llama3.2";

        return new OllamaChatClient(endpoint, modelId: modelId)
            .AsBuilder()
            .UseFunctionInvocation()
            .Build();
    }
}