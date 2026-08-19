using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;

namespace ClearMeasure.Bootcamp.McpServer;

/// <summary>
/// Resolves the loopback MCP HTTP endpoint from server address features.
/// </summary>
internal static class McpEndpointResolver
{
    internal static string ResolveMcpUrl(IServer server)
    {
        var addressFeature = server.Features.Get<IServerAddressesFeature>();
        var address = addressFeature?.Addresses.FirstOrDefault()
                      ?? throw new InvalidOperationException(
                          "Cannot determine server address for MCP loopback connection");

        return address.TrimEnd('/') + "/mcp";
    }
}
