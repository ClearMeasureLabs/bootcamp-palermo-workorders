using System.Security.Cryptography;
using System.Text;
using ClearMeasure.Bootcamp.UI.Shared;
using Microsoft.AspNetCore.Http;
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
        (ApiPublicPathRules.TryGetLeafSegment(pathValue, out var leaf) && ApiPublicPathRules.IsPublicLeaf(leaf))
        || ApiPublicPathRules.IsPublicToolsHashPath(pathValue);

    internal static bool IsAuthorized(HttpRequest request, string expectedKey)
    {
        if (!request.Headers.TryGetValue(ApiKeyConstants.HeaderName, out var providedValues))
        {
            return false;
        }

        var provided = providedValues.FirstOrDefault();
        return !string.IsNullOrEmpty(provided) && FixedTimeEqualsUtf8(expectedKey, provided);
    }

    internal static bool FixedTimeEqualsUtf8(string expected, string provided)
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

        if (segments.Length == 2)
        {
            leaf = segments[1];
            return true;
        }

        if (!segments[1].StartsWith('v'))
        {
            return false;
        }

        leaf = segments[2];
        return true;
    }

    internal static bool IsPublicLeaf(string leaf) =>
        leaf.Equals("version", StringComparison.OrdinalIgnoreCase)
        || leaf.Equals("time", StringComparison.OrdinalIgnoreCase)
        || leaf.Equals("ping", StringComparison.OrdinalIgnoreCase);

    internal static bool IsPublicToolsHashPath(string pathValue)
    {
        var segments = pathValue.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length < 3 || !segments[0].Equals("api", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (segments.Length == 3)
        {
            return segments[1].Equals("tools", StringComparison.OrdinalIgnoreCase)
                && segments[2].Equals("hash", StringComparison.OrdinalIgnoreCase);
        }

        if (segments.Length == 4 && segments[1].StartsWith('v'))
        {
            return segments[2].Equals("tools", StringComparison.OrdinalIgnoreCase)
                && segments[3].Equals("hash", StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }
}
