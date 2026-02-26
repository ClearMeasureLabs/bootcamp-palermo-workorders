using ClearMeasure.Bootcamp.IntegrationTests;
using ClearMeasure.Bootcamp.LlmGateway;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace ClearMeasure.Bootcamp.McpAcceptanceTests;

/// <summary>
/// Provides MCP tool invocation and LLM chat helpers for acceptance tests.
/// Uses the MCP HTTP endpoint hosted by UI.Server at /mcp.
/// </summary>
public class McpTestHelper(McpClient client, IList<McpClientTool> tools, ChatClientFactory factory)
{
    public ChatClientFactory Factory { get; } = factory;
    public IList<McpClientTool> Tools => tools;

    public async Task<string> CallToolDirectly(string toolName, Dictionary<string, object?> arguments)
    {
        var result = await client.CallToolAsync(toolName, arguments);
        return string.Join("\n", result.Content
            .OfType<TextContentBlock>()
            .Select(c => c.Text));
    }

    public async Task<ChatResponse> SendPrompt(string prompt)
    {
        var chatClient = await Factory.GetChatClient();
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System,
                "You are a helpful assistant with access to tools for managing work orders and employees. " +
                "Always use the provided tools to answer questions. Return the raw data from tool results."),
            new(ChatRole.User, prompt)
        };

        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        return await chatClient.GetResponseAsync(messages,
            new ChatOptions { Tools = [.. tools] },
            cts.Token);
    }

    public static string ExtractJsonValue(string json, string propertyName)
    {
        var searchPattern = $"\"{propertyName}\": \"";
        var startIndex = json.IndexOf(searchPattern, StringComparison.Ordinal);
        if (startIndex < 0) return string.Empty;

        startIndex += searchPattern.Length;
        var endIndex = json.IndexOf('"', startIndex);
        return endIndex < 0 ? string.Empty : json[startIndex..endIndex];
    }

    internal static string? GetLlmConfigValue(string key)
    {
        var configuration = TestHost.GetRequiredService<IConfiguration>();
        var value = configuration.GetValue<string>(key);
        if (!string.IsNullOrEmpty(value)) return value;

        return Environment.GetEnvironmentVariable(key);
    }

    internal static async Task<(bool Available, string Provider)> CheckLlmAvailability()
    {
        var apiKey = GetLlmConfigValue("AI_OpenAI_ApiKey");
        if (!string.IsNullOrEmpty(apiKey))
        {
            var url = GetLlmConfigValue("AI_OpenAI_Url");
            var model = GetLlmConfigValue("AI_OpenAI_Model");
            if (!string.IsNullOrEmpty(url) && !string.IsNullOrEmpty(model))
            {
                return (true, "AzureOpenAI");
            }
        }

        try
        {
            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            var response = await httpClient.GetAsync("http://localhost:11434/");
            return response.IsSuccessStatusCode ? (true, "Ollama") : (false, "None");
        }
        catch
        {
            return (false, "None");
        }
    }
}
