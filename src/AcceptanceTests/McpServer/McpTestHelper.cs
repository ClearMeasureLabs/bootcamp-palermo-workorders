using ClearMeasure.Bootcamp.AcceptanceTests;
using ClearMeasure.Bootcamp.IntegrationTests;
using ClearMeasure.Bootcamp.LlmGateway;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace ClearMeasure.Bootcamp.AcceptanceTests.McpServer;

/// <summary>
/// Provides MCP tool invocation and LLM chat helpers for acceptance tests.
/// Connects to the MCP HTTP endpoint hosted by UI.Server at /mcp.
/// </summary>
public class McpTestHelper(ChatClientFactory factory) : IAsyncDisposable
{
    private McpClient? _client;
    private IList<McpClientTool>? _tools;

    public IList<McpClientTool> Tools => _tools ?? throw new InvalidOperationException("Not connected to MCP server");
    public bool Connected => _client != null;

    public async Task ConnectAsync()
    {
        var mcpUrl = ServerFixture.ApplicationBaseUrl + "/mcp";
        TestContext.Out.WriteLine($"McpTestHelper: connecting to {mcpUrl}");

        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
        var httpClient = new HttpClient(handler);
        var transportOptions = new HttpClientTransportOptions
        {
            Endpoint = new Uri(mcpUrl),
            Name = "ChurchBulletin-HTTP"
        };
        var transport = new HttpClientTransport(transportOptions, httpClient);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        _client = await McpClient.CreateAsync(transport, cancellationToken: cts.Token);
        _tools = await _client.ListToolsAsync(cancellationToken: cts.Token);

        TestContext.Out.WriteLine($"McpTestHelper: connected, {_tools.Count} tools discovered");
    }

    public async Task<string> CallToolDirectly(string toolName, Dictionary<string, object?> arguments)
    {
        var result = await _client!.CallToolAsync(toolName, arguments);
        return string.Join("\n", result.Content
            .OfType<TextContentBlock>()
            .Select(c => c.Text));
    }

    public async Task<ChatResponse> SendPrompt(string prompt)
    {
        var chatClient = await factory.GetChatClient();
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System,
                "You are a helpful assistant with access to tools for managing work orders and employees. " +
                "Always use the provided tools to answer questions. Return the raw data from tool results."),
            new(ChatRole.User, prompt)
        };

        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        return await chatClient.GetResponseAsync(messages,
            new ChatOptions { Tools = [.. _tools!] },
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

    public async ValueTask DisposeAsync()
    {
        if (_client != null)
        {
            await _client.DisposeAsync();
            _client = null;
        }
    }
}
