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

        if (RequiresLlm && !McpServerFixture.OllamaAvailable)
            Assert.Inconclusive("Ollama LLM is not available at http://localhost:11434");
    }

    protected IChatClient BuildChatClient()
    {
        return new OllamaChatClient("http://localhost:11434/", modelId: "llama3.2")
            .AsBuilder()
            .UseFunctionInvocation()
            .Build();
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
}
