using ClearMeasure.Bootcamp.AgentProtocols;
using System.Net.Security;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace ClearMeasure.Bootcamp.WorkOrderAgentSpike;

/// <summary>HTTP MCP client used by the spike agent to invoke Church Bulletin work-order tools.</summary>
public sealed class McpWorkOrderClient : IMcpWorkOrderClient
{
    /// <summary>Invokes the named tool on the endpoint and returns its joined text output.</summary>
    public async Task<string> CallToolAsync(McpEndpointConfig endpoint, string toolName, IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        using var httpClient = CreateHttpClient(endpoint.Url);
        ApplyHeaders(httpClient, endpoint.Headers);
        var transport = new HttpClientTransport(new HttpClientTransportOptions { Endpoint = new Uri(endpoint.Url), Name = endpoint.Name }, httpClient);
        await using var client = await McpClient.CreateAsync(transport, cancellationToken: cancellationToken);
        var result = await client.CallToolAsync(toolName, arguments, cancellationToken: cancellationToken);
        return ExtractText(result.Content);
    }

    /// <summary>Creates an <see cref="HttpClient"/> that tolerates the local development TLS certificate on loopback URLs.</summary>
    private static HttpClient CreateHttpClient(string url)
    {
        var uri = new Uri(url);
        return new HttpClient(IsLoopbackHttps(uri) ? DevCertificateHandler() : new HttpClientHandler());
    }

    /// <summary>True when the URL is HTTPS and points at localhost or 127.0.0.1.</summary>
    private static bool IsLoopbackHttps(Uri uri)
        => uri.Scheme == Uri.UriSchemeHttps && IsLoopbackHost(uri.Host);

    /// <summary>True when the host is a loopback name.</summary>
    private static bool IsLoopbackHost(string host)
        => string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase) || host == "127.0.0.1";

    /// <summary>Handler that accepts the untrusted development certificate used on loopback HTTPS.</summary>
    private static HttpClientHandler DevCertificateHandler()
        => new() { ServerCertificateCustomValidationCallback = (_, _, _, errors) => errors is SslPolicyErrors.None or SslPolicyErrors.RemoteCertificateChainErrors };

    /// <summary>Adds every configured header to the client's default request headers.</summary>
    private static void ApplyHeaders(HttpClient httpClient, IReadOnlyList<AcpMcpHttpHeader> headers)
    {
        foreach (var header in headers)
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation(header.Name, header.Value);
    }

    /// <summary>Joins all text content blocks of a tool result with newlines.</summary>
    private static string ExtractText(IEnumerable<ContentBlock> content)
        => string.Join("\n", content.OfType<TextContentBlock>().Select(b => b.Text));
}
