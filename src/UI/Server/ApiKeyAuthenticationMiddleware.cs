using System.Security.Cryptography;
using System.Text;
using ClearMeasure.Bootcamp.UI.Shared;
using Microsoft.Extensions.Options;

namespace ClearMeasure.Bootcamp.UI.Server;

public sealed class ApiKeyAuthenticationMiddleware(RequestDelegate next)
{
    public Task InvokeAsync(HttpContext context, IOptions<ApiKeyAuthenticationOptions> optionsAccessor) =>
        ApiKeyAuthenticationPipeline.InvokeAsync(context, next, optionsAccessor.Value);

    internal static bool ShouldValidate(PathString path, ApiKeyAuthenticationOptions options) =>
        ApiKeyValidationRules.RequiresValidation(path.Value, options);

    internal static bool IsApiPath(string pathValue) =>
        pathValue.StartsWith("/api/", StringComparison.OrdinalIgnoreCase)
        || pathValue.Equals("/api", StringComparison.OrdinalIgnoreCase);

    internal static bool IsPublicVersionOrTimePath(string pathValue) =>
        ApiPublicPathRules.TryGetLeafSegment(pathValue, out var leaf) && ApiPublicPathRules.IsPublicLeaf(leaf);

    internal static bool IsAuthorized(HttpRequest request, string expectedKey)
    {
        if (!request.Headers.TryGetValue(ApiKeyConstants.HeaderName, out var providedValues))
        {
            return false;
        }

        var provided = providedValues.FirstOrDefault();
        return !string.IsNullOrEmpty(provided) && FixedTimeEqualsUtf8(expectedKey, provided);
    }

    private static bool FixedTimeEqualsUtf8(string expected, string provided)
    {
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        var providedBytes = Encoding.UTF8.GetBytes(provided);
        if (expectedBytes.Length != providedBytes.Length)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(expectedBytes, providedBytes);
    }
}

internal static class ApiKeyAuthenticationPipeline
{
    internal static async Task InvokeAsync(
        HttpContext context,
        RequestDelegate next,
        ApiKeyAuthenticationOptions options)
    {
        if (!ApiKeyValidationRules.RequiresValidation(context.Request.Path.Value, options))
        {
            await next(context);
            return;
        }

        if (!ApiKeyAuthenticationMiddleware.IsAuthorized(context.Request, options.ValidationKey ?? string.Empty))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        await next(context);
    }
}

internal static class ApiKeyValidationRules
{
    internal static bool RequiresValidation(string? pathValue, ApiKeyAuthenticationOptions options)
    {
        if (!options.Enabled || string.IsNullOrWhiteSpace(options.ValidationKey))
        {
            return false;
        }

        return !string.IsNullOrEmpty(pathValue)
               && ApiKeyAuthenticationMiddleware.IsApiPath(pathValue)
               && !ApiKeyAuthenticationMiddleware.IsPublicVersionOrTimePath(pathValue);
    }
}

internal static class ApiPublicPathRules
{
    internal static bool TryGetLeafSegment(string pathValue, out string leaf)
    {
        leaf = string.Empty;
        var segments = pathValue.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length < 2 || !segments[0].Equals("api", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        leaf = ResolveApiLeaf(segments);
        return leaf.Length > 0;
    }

    private static string ResolveApiLeaf(string[] segments)
    {
        if (segments.Length == 2)
        {
            return segments[1];
        }

        return segments[1].StartsWith('v')
            ? string.Join('/', segments.Skip(2))
            : string.Join('/', segments.Skip(1));
    }

    internal static bool IsPublicLeaf(string leaf) =>
        leaf.Equals("version", StringComparison.OrdinalIgnoreCase)
        || leaf.Equals("time", StringComparison.OrdinalIgnoreCase)
        || leaf.Equals("ping", StringComparison.OrdinalIgnoreCase)
        || leaf.Equals("echo", StringComparison.OrdinalIgnoreCase)
        || leaf.Equals("tools/random", StringComparison.OrdinalIgnoreCase)
        || leaf.Equals("tools/timestamp-converter", StringComparison.OrdinalIgnoreCase);
}
