using System.Security.Cryptography;
using System.Text;
using ClearMeasure.Bootcamp.UI.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace ClearMeasure.Bootcamp.UI.Server;

/// <summary>
/// Enforces an optional shared API key on <c>/api/*</c> routes, excluding public version, time, and ping endpoints.
/// </summary>
public sealed class ApiKeyAuthenticationMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IOptions<ApiKeyAuthenticationOptions> optionsAccessor)
    {
        var options = optionsAccessor.Value;
        if (!ShouldValidate(context.Request.Path, options))
        {
            await next(context);
            return;
        }

        if (!IsAuthorized(context.Request, options.ValidationKey ?? string.Empty))
        {
            await WriteUnauthorizedAsync(context);
            return;
        }

        await next(context);
    }

    internal static bool ShouldValidate(PathString path, ApiKeyAuthenticationOptions options)
    {
        if (!options.Enabled || string.IsNullOrWhiteSpace(options.ValidationKey))
        {
            return false;
        }

        var value = path.Value;
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        if (!IsApiPath(value))
        {
            return false;
        }

        return !IsPublicVersionOrTimePath(value);
    }

    internal static bool IsApiPath(string pathValue) =>
        pathValue.StartsWith("/api/", StringComparison.OrdinalIgnoreCase)
        || pathValue.Equals("/api", StringComparison.OrdinalIgnoreCase);

    internal static bool IsPublicVersionOrTimePath(string pathValue)
    {
        if (!TryGetApiLeafSegment(pathValue, out var leaf))
        {
            return false;
        }

        return IsPublicApiLeaf(leaf);
    }

    internal static bool TryGetApiLeafSegment(string pathValue, out string leaf)
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

        if (segments.Length >= 3 && segments[1].StartsWith('v'))
        {
            leaf = segments[2];
            return true;
        }

        return false;
    }

    internal static bool IsPublicApiLeaf(string leaf) =>
        leaf.Equals("version", StringComparison.OrdinalIgnoreCase)
        || leaf.Equals("time", StringComparison.OrdinalIgnoreCase)
        || leaf.Equals("ping", StringComparison.OrdinalIgnoreCase);

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

    private static Task WriteUnauthorizedAsync(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    }
}
