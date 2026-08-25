using ClearMeasure.Bootcamp.LlmGateway;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;

namespace ClearMeasure.Bootcamp.McpServer;

/// <summary>
/// Discovers AI tools by connecting to the co-hosted MCP HTTP endpoint at /mcp.
/// Replaces the previous manual wrapper approach with a loopback MCP client.
/// </summary>
public class ToolProvider(
    IServer server,
    IHttpClientFactory httpClientFactory,
    ILogger<ToolProvider> logger) : IToolProvider, IAsyncDisposable
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private McpClient? _client;
    private IList<AITool>? _tools;

    public async Task<IList<AITool>> GetToolsAsync()
    {
        if (_tools != null)
            return _tools;

        await _lock.WaitAsync();
        try
        {
            if (_tools != null)
                return _tools;

            _tools = await DiscoverToolsAsync();
            return _tools;
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<IList<AITool>> DiscoverToolsAsync()
    {
        var mcpUrl = McpEndpointResolver.ResolveMcpUrl(server);
        logger.LogInformation("ToolProvider: connecting to MCP endpoint at {McpUrl}", mcpUrl);

        var httpClient = CreateLoopbackHttpClient(mcpUrl);
        var transportOptions = new HttpClientTransportOptions
        {
            Endpoint = new Uri(mcpUrl),
            Name = "ChurchBulletin-Loopback"
        };
        var transport = new HttpClientTransport(transportOptions, httpClient);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var localClient = await McpClient.CreateAsync(transport, cancellationToken: cts.Token);

        try
        {
            var mcpTools = await localClient.ListToolsAsync(cancellationToken: cts.Token);
            _client = localClient;
            var tools = mcpTools.Cast<AITool>().ToList();
            logger.LogInformation("ToolProvider: discovered {ToolCount} tools via MCP", tools.Count);
            return tools;
        }
        catch
        {
            await localClient.DisposeAsync();
            throw;
        }
    }

    /// <summary>
    /// Loopback HTTPS uses the ASP.NET dev certificate; accept it for in-process MCP discovery.
    /// Prefer the shared factory client for plain http:// endpoints.
    /// </summary>
    private HttpClient CreateLoopbackHttpClient(string mcpUrl)
    {
        if (!mcpUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return httpClientFactory.CreateClient();
        }

        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
        return new HttpClient(handler);
    }

    public async ValueTask DisposeAsync()
    {
        if (_client != null)
        {
            await _client.DisposeAsync();
            _client = null;
        }
        _lock.Dispose();
    }
}
