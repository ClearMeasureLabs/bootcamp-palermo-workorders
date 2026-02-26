using Azure;
using Azure.AI.OpenAI;
using ClearMeasure.Bootcamp.Core;
using Microsoft.Extensions.AI;
using OpenAI.Chat;

namespace ClearMeasure.Bootcamp.LlmGateway;

public class ChatClientFactory(IBus bus)
{
    public async Task<ChatClientAvailabilityResult> IsChatClientAvailable()
    {
        var config = await bus.Send(new ChatClientConfigQuery());
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

    public virtual async Task<IChatClient> GetChatClient()
    {
        var config = await bus.Send(new ChatClientConfigQuery());
        var apiKey = config.AiOpenAiApiKey
            ?? throw new InvalidOperationException("AI_OpenAI_ApiKey is not configured.");

        IChatClient innerClient = BuildAzureOpenAiChatClient(config, apiKey);

        return new TracingChatClient(innerClient);
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
