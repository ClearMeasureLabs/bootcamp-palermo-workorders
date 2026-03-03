using ClearMeasure.Bootcamp.LlmGateway;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;

namespace ClearMeasure.Bootcamp.McpServer;

/// <summary>
/// Discovers AI tools by connecting to the co-hosted MCP HTTP endpoint at /mcp.
/// Replaces the previous manual wrapper approach with a loopback MCP client.
/// </summary>
public class ToolProvider(IServer server, ILogger<ToolProvider> logger) : IToolProvider, IAsyncDisposable
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

            var addressFeature = server.Features.Get<IServerAddressesFeature>();
            var address = addressFeature?.Addresses.FirstOrDefault()
                          ?? throw new InvalidOperationException(
                              "Cannot determine server address for MCP loopback connection");

            var mcpUrl = address.TrimEnd('/') + "/mcp";
            logger.LogInformation("ToolProvider: connecting to MCP endpoint at {McpUrl}", mcpUrl);

            var httpClient = new HttpClient();
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
                _tools = mcpTools.Cast<AITool>().ToList();
            }
            catch
            {
                await localClient.DisposeAsync();
                throw;
            }

            logger.LogInformation("ToolProvider: discovered {ToolCount} tools via MCP", _tools.Count);
            return _tools;
        }
        finally
        {
            _lock.Release();
        }
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
