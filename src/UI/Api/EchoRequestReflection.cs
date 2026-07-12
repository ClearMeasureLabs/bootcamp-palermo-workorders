using Microsoft.AspNetCore.Http;

namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Builds diagnostic header maps for <see cref="EchoResponse"/> without exposing secrets.
/// </summary>
internal static class EchoRequestReflection
{
    private const string ApiKeyHeaderName = "X-Api-Key";
    private const string ApiKeyRedactedValue = "[present]";

    private static readonly HashSet<string> HopByHopHeaderNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Connection",
        "Keep-Alive",
        "Proxy-Authenticate",
        "Proxy-Authorization",
        "TE",
        "Trailer",
        "Transfer-Encoding",
        "Upgrade"
    };

    private static readonly HashSet<string> ExcludedHeaderNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Authorization",
        "Cookie",
        "Set-Cookie"
    };

    private static readonly HashSet<string> DiagnosticHeaderNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Accept",
        "Accept-Encoding",
        "Accept-Language",
        "User-Agent",
        "Content-Type",
        ApiKeyHeaderName,
        "X-Correlation-ID",
        "X-Forwarded-For",
        "X-Forwarded-Proto",
        "X-Forwarded-Host",
        "Origin",
        "Referer",
        "If-None-Match",
        "Cache-Control"
    };

    /// <summary>
    /// Returns the first query-string value per key from the request.
    /// </summary>
    public static IReadOnlyDictionary<string, string> BuildQuery(IQueryCollection query)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in query)
        {
            result[pair.Key] = pair.Value.ToString();
        }

        return result;
    }

    /// <summary>
    /// Returns selected diagnostic headers with sensitive values redacted or omitted.
    /// </summary>
    public static IReadOnlyDictionary<string, string> BuildHeaders(IHeaderDictionary headers)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in headers)
        {
            if (HopByHopHeaderNames.Contains(pair.Key) || ExcludedHeaderNames.Contains(pair.Key))
            {
                continue;
            }

            if (!DiagnosticHeaderNames.Contains(pair.Key))
            {
                continue;
            }

            if (pair.Key.Equals(ApiKeyHeaderName, StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrEmpty(pair.Value.ToString()))
                {
                    result[pair.Key] = ApiKeyRedactedValue;
                }

                continue;
            }

            var value = pair.Value.ToString();
            if (!string.IsNullOrEmpty(value))
            {
                result[pair.Key] = value;
            }
        }

        return result;
    }
}
