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

        if (!context.Request.Headers.TryGetValue(ApiKeyConstants.HeaderName, out var providedValues))
        {
            await WriteUnauthorizedAsync(context);
            return;
        }

        var provided = providedValues.FirstOrDefault();
        var expected = options.ValidationKey ?? string.Empty;
        if (string.IsNullOrEmpty(provided) || !FixedTimeEqualsUtf8(expected, provided))
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

        if (!value.StartsWith("/api/", StringComparison.OrdinalIgnoreCase)
            && !value.Equals("/api", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (IsPublicVersionOrTimePath(value))
        {
            return false;
        }

        return true;
    }

    internal static bool IsPublicVersionOrTimePath(string pathValue)
    {
        var segments = pathValue.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length < 2 || !segments[0].Equals("api", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (segments.Length == 2)
        {
            var leaf = segments[1];
            return IsPublicApiLeaf(leaf);
        }

        if (segments.Length == 3
            && segments[1].Equals("status", StringComparison.OrdinalIgnoreCase))
        {
            return segments[2].Equals("environment", StringComparison.OrdinalIgnoreCase);
        }

        if (segments.Length >= 3
            && segments[1].StartsWith("v", StringComparison.OrdinalIgnoreCase))
        {
            if (segments.Length >= 4
                && segments[2].Equals("status", StringComparison.OrdinalIgnoreCase))
            {
                return segments[3].Equals("environment", StringComparison.OrdinalIgnoreCase);
            }

            var leaf = segments[2];
            return IsPublicApiLeaf(leaf);
        }

        return false;
    }

    private static bool IsPublicApiLeaf(string leaf) =>
        leaf.Equals("version", StringComparison.OrdinalIgnoreCase)
        || leaf.Equals("time", StringComparison.OrdinalIgnoreCase)
        || leaf.Equals("ping", StringComparison.OrdinalIgnoreCase);

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

    private static Task WriteUnauthorizedAsync(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    }
}
