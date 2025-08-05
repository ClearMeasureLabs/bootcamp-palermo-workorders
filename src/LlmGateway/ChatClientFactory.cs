using Azure;
using Azure.AI.OpenAI;
using ClearMeasure.Bootcamp.Core;
using Microsoft.Extensions.AI;
using OpenAI.Chat;

namespace ClearMeasure.Bootcamp.LlmGateway;

public class ChatClientFactory()
{
    public async Task<IChatClient> GetChatClient()
    {
		return await Task.FromResult(BuildOllamaChatClient());
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