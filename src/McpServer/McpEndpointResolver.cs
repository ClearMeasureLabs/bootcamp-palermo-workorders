using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;

namespace ClearMeasure.Bootcamp.McpServer;

/// <summary>
/// Resolves the loopback MCP HTTP endpoint from server address features.
/// Prefers http:// so the in-process ToolProvider avoids HTTPS dev-cert trust failures.
/// </summary>
internal static class McpEndpointResolver
{
    internal static string ResolveMcpUrl(IServer server)
    {
        var addressFeature = server.Features.Get<IServerAddressesFeature>();
        var addresses = addressFeature?.Addresses?.ToList()
                        ?? throw new InvalidOperationException(
                            "Cannot determine server address for MCP loopback connection");

        if (addresses.Count == 0)
        {
            throw new InvalidOperationException(
                "Cannot determine server address for MCP loopback connection");
        }

        var address = addresses.FirstOrDefault(a =>
                          a.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
                      ?? addresses[0];

        return address.TrimEnd('/') + "/mcp";
    }
}
