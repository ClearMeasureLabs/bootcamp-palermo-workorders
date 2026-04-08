using Azure;
using Azure.AI.OpenAI;
using ClearMeasure.Bootcamp.Core;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;

namespace ClearMeasure.Bootcamp.LlmGateway;

public class ChatClientFactory(IBus bus)
{
    private const string OpenAiCompatibleProvider = "OpenAICompatible";

    public async Task<ChatClientAvailabilityResult> IsChatClientAvailable()
    {
        var config = await bus.Send(new ChatClientConfigQuery());
        var missing = new List<string>();

        if (string.IsNullOrEmpty(config.AiOpenAiApiKey)) missing.Add("AI_OpenAI_ApiKey");
        if (string.IsNullOrEmpty(config.AiOpenAiUrl)) missing.Add("AI_OpenAI_Url");
        if (string.IsNullOrEmpty(config.AiOpenAiModel)) missing.Add("AI_OpenAI_Model");

        if (missing.Count > 0)
        {
            var modeHint = IsOpenAiCompatibleMode(config)
                ? " (OpenAI-compatible mode: set AI_OpenAI_Provider=OpenAICompatible and use a base URL such as https://api.openai.com/v1)"
                : " (Azure OpenAI: set AI_OpenAI_Url to the resource endpoint, e.g. https://your-resource.openai.azure.com)";
            return new ChatClientAvailabilityResult(false,
                $"Chat client is not configured. Set the following environment variables: {string.Join(", ", missing)}{modeHint}");
        }

        return new ChatClientAvailabilityResult(true, "Chat client is configured");
    }

    public async Task<IChatClient> GetChatClient()
    {
        var config = await bus.Send(new ChatClientConfigQuery());
        var apiKey = config.AiOpenAiApiKey
            ?? throw new InvalidOperationException("AI_OpenAI_ApiKey is not configured.");

        IChatClient innerClient = IsOpenAiCompatibleMode(config)
            ? BuildOpenAiCompatibleChatClient(config, apiKey)
            : BuildAzureOpenAiChatClient(config, apiKey);

        return new TracingChatClient(innerClient);
    }

    private static bool IsOpenAiCompatibleMode(ChatClientConfig config) =>
        string.Equals(config.AiOpenAiProvider, OpenAiCompatibleProvider, StringComparison.OrdinalIgnoreCase);

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

    private static IChatClient BuildOpenAiCompatibleChatClient(ChatClientConfig config, string apiKey)
    {
        var baseUrl = config.AiOpenAiUrl ?? throw new InvalidOperationException();
        var model = config.AiOpenAiModel ?? throw new InvalidOperationException();
        var credential = new ApiKeyCredential(apiKey);

        var options = new OpenAIClientOptions { Endpoint = new Uri(baseUrl) };
        var client = new OpenAIClient(credential, options);
        var chatClient = client.GetChatClient(model);
        return chatClient.AsIChatClient()
            .AsBuilder()
            .UseFunctionInvocation()
            .Build();
    }
}
