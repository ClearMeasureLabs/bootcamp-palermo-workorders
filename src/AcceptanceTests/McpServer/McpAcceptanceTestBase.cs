using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace ClearMeasure.Bootcamp.McpAcceptanceTests;

public abstract class McpAcceptanceTestBase
{
    protected McpClient Client => McpServerFixture.McpClientInstance!;
    protected IList<McpClientTool> Tools => McpServerFixture.Tools!;

    protected bool RequiresLlm { get; set; } = true;

    [SetUp]
    public void EnsureAvailability()
    {
        if (!McpServerFixture.ServerAvailable)
            Assert.Inconclusive("MCP server is not available");

        if (RequiresLlm && !McpServerFixture.LlmAvailable)
            Assert.Inconclusive("No LLM available (set AI_OpenAI_ApiKey/Url/Model or run Ollama locally)");
    }

    protected IChatClient BuildChatClient()
    {
        var apiKey = McpServerFixture.GetLlmConfigValue("AI_OpenAI_ApiKey");
        if (!string.IsNullOrEmpty(apiKey))
        {
            return BuildAzureOpenAiChatClient(apiKey);
        }

        return BuildOllamaChatClient();
    }

    protected async Task<ChatResponse> SendPrompt(string prompt)
    {
        var chatClient = BuildChatClient();
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System,
                "You are a helpful assistant with access to tools for managing work orders and employees. " +
                "Always use the provided tools to answer questions. Return the raw data from tool results."),
            new(ChatRole.User, prompt)
        };

        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        return await chatClient.GetResponseAsync(messages,
            new ChatOptions { Tools = [.. Tools] },
            cts.Token);
    }

    protected async Task<string> CallToolDirectly(string toolName, Dictionary<string, object?> arguments)
    {
        var result = await Client.CallToolAsync(toolName, arguments);
        return string.Join("\n", result.Content
            .OfType<TextContentBlock>()
            .Select(c => c.Text));
    }

    private static IChatClient BuildAzureOpenAiChatClient(string apiKey)
    {
        var url = McpServerFixture.GetLlmConfigValue("AI_OpenAI_Url")
                  ?? throw new InvalidOperationException("AI_OpenAI_Url is required when AI_OpenAI_ApiKey is set");
        var model = McpServerFixture.GetLlmConfigValue("AI_OpenAI_Model")
                    ?? throw new InvalidOperationException("AI_OpenAI_Model is required when AI_OpenAI_ApiKey is set");

        var credential = new AzureKeyCredential(apiKey);
        var openAiClient = new AzureOpenAIClient(new Uri(url), credential);
        var chatClient = openAiClient.GetChatClient(model);

        return chatClient.AsIChatClient()
            .AsBuilder()
            .UseFunctionInvocation()
            .Build();
    }

    private static IChatClient BuildOllamaChatClient()
    {
        return new OllamaChatClient("http://localhost:11434/", modelId: "llama3.2")
            .AsBuilder()
            .UseFunctionInvocation()
            .Build();
    }
}
