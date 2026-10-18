using Microsoft.AspNetCore.Http;

namespace ChurchBulletin.ServiceDefaults;

internal static class CorrelationIdResolver
{
    public static string Resolve(HttpContext context)
    {
        if (TryGetValidHeaderValue(context, out var id))
        {
            return id;
        }

        return Guid.NewGuid().ToString("D");
    }

    private static bool TryGetValidHeaderValue(HttpContext context, out string correlationId)
    {
        correlationId = string.Empty;
        if (!context.Request.Headers.TryGetValue(CorrelationIdConstants.HeaderName, out var fromHeader))
        {
            return false;
        }

        var id = fromHeader.ToString().Trim();
        if (id.Length == 0 || id.Length > CorrelationIdConstants.MaxLength)
        {
            return false;
        }

        correlationId = id;
        return true;
    }
}
