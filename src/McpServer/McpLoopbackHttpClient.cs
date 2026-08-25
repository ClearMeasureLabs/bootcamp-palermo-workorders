using System.Net.Security;

namespace ClearMeasure.Bootcamp.McpServer;

/// <summary>
/// Builds the HTTP client used for in-process MCP discovery over loopback HTTPS.
/// </summary>
internal static class McpLoopbackHttpClient
{
    /// <summary>
    /// Returns a client for loopback HTTPS that accepts any chain-validation failure, which covers
    /// the untrusted ASP.NET dev certificate, while still requiring a certificate that matches the
    /// host. Returns null for any other endpoint so callers use the shared factory client and normal
    /// certificate errors surface.
    /// </summary>
    internal static HttpClient? CreateForDevCertificate(string mcpUrl)
    {
        var mcpUri = new Uri(mcpUrl);
        if (mcpUri.Scheme != Uri.UriSchemeHttps || !mcpUri.IsLoopback)
        {
            return null;
        }

        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, errors) => IsAcceptable(errors)
        };
        return new HttpClient(handler);
    }

    internal static bool IsAcceptable(SslPolicyErrors errors) =>
        errors is SslPolicyErrors.None or SslPolicyErrors.RemoteCertificateChainErrors;
}
