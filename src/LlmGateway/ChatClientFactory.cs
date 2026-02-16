using System.Diagnostics;
using Azure;
using Azure.AI.OpenAI;
using ClearMeasure.Bootcamp.Core;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OpenAI.Chat;

namespace ClearMeasure.Bootcamp.LlmGateway;

public class ChatClientFactory(IBus bus, IConfiguration configuration)
{
    private static readonly ActivitySource LlmActivitySource = new(TelemetryConstants.LlmGatewaySourceName);

    public async Task<IChatClient> GetChatClient()
    {
        using var activity = LlmActivitySource.StartActivity("ChatClientFactory.GetChatClient");

        var config = await bus.Send(new ChatClientConfigQuery());
        var apiKey = config.AiOpenAiApiKey;
        if (string.IsNullOrEmpty(apiKey))
        {
            var endpoint = configuration.GetConnectionString("Ollama") ?? "http://localhost:11434/";
            var modelId = "llama3.2";

            activity?.SetTag("llm.provider", "ollama");
            activity?.SetTag("llm.model", modelId);
            activity?.SetTag("llm.endpoint", endpoint);

            return BuildOllamaChatClient(endpoint, modelId);
        }
        else
        {
            var openAiUrl = config.AiOpenAiUrl;
            var openAiModel = config.AiOpenAiModel;

            activity?.SetTag("llm.provider", "azure_openai");
            activity?.SetTag("llm.model", openAiModel);
            activity?.SetTag("llm.endpoint", openAiUrl);

            var credential = new AzureKeyCredential(apiKey ?? throw new InvalidOperationException());
            var uri = new Uri(openAiUrl ?? throw new InvalidOperationException());
            var openAiClient = new AzureOpenAIClient(uri, credential);

            OpenAI.Chat.ChatClient chatClient = openAiClient.GetChatClient(openAiModel);
            return chatClient.AsIChatClient();
        }
    }

    private static IChatClient BuildOllamaChatClient(string endpoint, string modelId)
    {
        return new OllamaChatClient(endpoint, modelId: modelId)
            .AsBuilder()
            .UseFunctionInvocation()
            .Build();
    }
}