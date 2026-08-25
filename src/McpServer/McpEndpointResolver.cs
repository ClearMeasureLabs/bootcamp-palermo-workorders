using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;

namespace ClearMeasure.Bootcamp.McpServer;

/// <summary>
/// Resolves the MCP endpoint for in-process discovery from server address features.
/// Prefers loopback and plain http:// so the call stays on the local interface and avoids
/// HTTPS dev-cert trust failures.
/// </summary>
internal static class McpEndpointResolver
{
    internal static string ResolveMcpUrl(IServer server)
    {
        var addressFeature = server.Features.Get<IServerAddressesFeature>();
        var addresses = addressFeature?.Addresses.ToList()
                        ?? throw new InvalidOperationException(
                            "Cannot determine server address for MCP loopback connection");

        if (addresses.Count == 0)
        {
            throw new InvalidOperationException(
                "Cannot determine server address for MCP loopback connection");
        }

        var address = SelectPreferredAddress(addresses);
        return address.TrimEnd('/') + "/mcp";
    }

    private static string SelectPreferredAddress(IReadOnlyList<string> addresses) =>
        addresses.FirstOrDefault(a => IsHttp(a) && IsLoopback(a))
        ?? addresses.FirstOrDefault(IsLoopback)
        ?? addresses.FirstOrDefault(IsHttp)
        ?? addresses[0];

    private static bool IsHttp(string address) =>
        address.StartsWith("http://", StringComparison.OrdinalIgnoreCase);

    private static bool IsLoopback(string address) =>
        Uri.TryCreate(address, UriKind.Absolute, out var uri) && uri.IsLoopback;
}
