using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;

namespace ClearMeasure.Bootcamp.McpServer;

/// <summary>
/// Resolves the MCP endpoint for in-process discovery from server address features.
/// Prefers loopback and plain http:// so the call stays on the local interface and avoids
/// HTTPS dev-cert trust failures. Rewrites unspecified bind hosts (0.0.0.0 / ::) to
/// loopback so HttpClient can dial the co-hosted endpoint.
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

        var address = ToClientReachableAddress(SelectPreferredAddress(addresses));
        return address.TrimEnd('/') + "/mcp";
    }

    private static string SelectPreferredAddress(IReadOnlyList<string> addresses) =>
        addresses.FirstOrDefault(a => IsHttp(a) && IsLoopback(a))
        ?? addresses.FirstOrDefault(IsLoopback)
        ?? addresses.FirstOrDefault(IsHttp)
        ?? addresses[0];

    /// <summary>
    /// Maps Kestrel bind-any hosts to loopback while preserving scheme and port.
    /// Unspecified addresses cannot be used as HttpClient targets.
    /// </summary>
    internal static string ToClientReachableAddress(string address)
    {
        if (!Uri.TryCreate(address, UriKind.Absolute, out var uri))
        {
            return RewriteUnparseableWildcard(address);
        }

        if (!IPAddress.TryParse(uri.IdnHost, out var ip) || !IsUnspecified(ip))
        {
            return address.TrimEnd('/');
        }

        var loopbackHost = ip.AddressFamily == AddressFamily.InterNetworkV6
            ? IPAddress.IPv6Loopback.ToString()
            : IPAddress.Loopback.ToString();

        var builder = new UriBuilder(uri) { Host = loopbackHost };
        return builder.Uri.GetLeftPart(UriPartial.Authority).TrimEnd('/');
    }

    private static string RewriteUnparseableWildcard(string address)
    {
        // Kestrel may advertise http://+:port or http://*:port, which Uri rejects.
        foreach (var token in new[] { "://+:", "://*:" })
        {
            var index = address.IndexOf(token, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
            {
                continue;
            }

            var scheme = address[..index];
            var portAndRest = address[(index + token.Length)..].TrimEnd('/');
            var port = portAndRest.Split('/', 2)[0];
            if (int.TryParse(port, out _))
            {
                return $"{scheme}://{IPAddress.Loopback}:{port}";
            }
        }

        return address.TrimEnd('/');
    }

    private static bool IsUnspecified(IPAddress ip) =>
        IPAddress.Any.Equals(ip) || IPAddress.IPv6Any.Equals(ip);

    private static bool IsHttp(string address) =>
        address.StartsWith("http://", StringComparison.OrdinalIgnoreCase);

    private static bool IsLoopback(string address) =>
        Uri.TryCreate(address, UriKind.Absolute, out var uri) && uri.IsLoopback;
}
