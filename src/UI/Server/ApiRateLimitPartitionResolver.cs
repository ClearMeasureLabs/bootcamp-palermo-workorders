
namespace ClearMeasure.Bootcamp.UI.Server;

internal static class ApiRateLimitPartitionResolver
{
    internal static string Resolve(HttpContext httpContext, string apiKeyHeaderName)
    {
        if (TryGetKeyPartition(httpContext, apiKeyHeaderName, out var keyPartition))
        {
            return keyPartition;
        }

        if (TryGetUserPartition(httpContext, out var userPartition))
        {
            return userPartition;
        }

        return TryGetIpPartition(httpContext, out var ipPartition) ? ipPartition : "anonymous";
    }

    internal static bool TryGetKeyPartition(HttpContext httpContext, string apiKeyHeaderName, out string partition)
    {
        partition = string.Empty;
        if (string.IsNullOrWhiteSpace(apiKeyHeaderName)
            || !httpContext.Request.Headers.TryGetValue(apiKeyHeaderName, out var keyValues))
        {
            return false;
        }

        var key = keyValues.ToString();
        if (string.IsNullOrWhiteSpace(key))
        {
            return false;
        }

        partition = "key:" + key;
        return true;
    }

    internal static bool TryGetUserPartition(HttpContext httpContext, out string partition)
    {
        partition = string.Empty;
        var userName = httpContext.User.Identity?.Name;
        if (string.IsNullOrEmpty(userName))
        {
            return false;
        }

        partition = "user:" + userName;
        return true;
    }

    internal static bool TryGetIpPartition(HttpContext httpContext, out string partition)
    {
        partition = string.Empty;
        var remoteIp = httpContext.Connection.RemoteIpAddress?.ToString();
        if (string.IsNullOrEmpty(remoteIp))
        {
            return false;
        }

        partition = "ip:" + remoteIp;
        return true;
    }
}
