using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace ClearMeasure.Bootcamp.UI.Server.Middleware;

/// <summary>
/// Replays the first successful (2xx) response for duplicate POST or PUT requests to <c>/api/*</c> (and the Blazor WASM
/// single-API paths) that share the same <see cref="IdempotencyConstants.HeaderName"/> and the same method, path, and body.
/// </summary>
public sealed class IdempotencyMiddleware
{
    private const string BindingPrefix = "__idempotency_binding:";

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

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

    private readonly RequestDelegate _next;
    private readonly IMemoryCache _cache;
    private readonly IOptionsMonitor<IdempotencyOptions> _optionsMonitor;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _keyLocks = new(StringComparer.Ordinal);

    public IdempotencyMiddleware(
        RequestDelegate next,
        IMemoryCache cache,
        IOptionsMonitor<IdempotencyOptions> optionsMonitor)
    {
        _next = next;
        _cache = cache;
        _optionsMonitor = optionsMonitor;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!ShouldInspect(context.Request))
        {
            await _next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue(IdempotencyConstants.HeaderName, out var keyValues))
        {
            await _next(context);
            return;
        }

        var idempotencyKey = keyValues.ToString().Trim();
        if (string.IsNullOrEmpty(idempotencyKey))
        {
            await _next(context);
            return;
        }

        var opts = _optionsMonitor.CurrentValue;
        if (idempotencyKey.Length > opts.MaxKeyLength)
        {
            await WriteBadRequestAsync(context, $"Idempotency key exceeds maximum length of {opts.MaxKeyLength}.");
            return;
        }

        context.Request.EnableBuffering();
        context.Request.Body.Position = 0;
        var bodyHash = await ComputeBodySha256HexAsync(context.Request.Body, context.RequestAborted);
        context.Request.Body.Position = 0;

        var compositeKey =
            $"{context.Request.Method}\u001f{context.Request.Path.Value}\u001f{idempotencyKey}\u001f{bodyHash}";

        if (_cache.TryGetValue(compositeKey, out IdempotentResponseSnapshot? cachedSnapshot) && cachedSnapshot is not null)
        {
            await ReplayCachedResponseAsync(context, cachedSnapshot);
            return;
        }

        var sem = _keyLocks.GetOrAdd(idempotencyKey, static _ => new SemaphoreSlim(1, 1));
        await sem.WaitAsync(context.RequestAborted);
        try
        {
            if (_cache.TryGetValue(compositeKey, out cachedSnapshot) && cachedSnapshot is not null)
            {
                await ReplayCachedResponseAsync(context, cachedSnapshot);
                return;
            }

            if (_cache.TryGetValue(BindingPrefix + idempotencyKey, out string? boundComposite)
                && !string.Equals(boundComposite, compositeKey, StringComparison.Ordinal))
            {
                await WriteConflictAsync(context);
                return;
            }

            var originalBody = context.Response.Body;
            await using var buffer = new MemoryStream();
            context.Response.Body = buffer;

            try
            {
                await _next(context);
            }
            finally
            {
                context.Response.Body = originalBody;
            }

            var status = context.Response.StatusCode;
            if (status is >= 200 and < 300)
            {
                var bodyBytes = buffer.ToArray();
                var headers = CaptureResponseHeaders(context.Response);
                var snapshot = new IdempotentResponseSnapshot(status, headers, bodyBytes);
                var cacheSeconds = Math.Max(1, opts.CacheEntrySeconds);
                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(cacheSeconds)
                };
                _cache.Set(compositeKey, snapshot, cacheOptions);
                _cache.Set(BindingPrefix + idempotencyKey, compositeKey, cacheOptions);
            }

            buffer.Position = 0;
            context.Response.ContentLength = buffer.Length;
            await buffer.CopyToAsync(originalBody, context.RequestAborted);
        }
        finally
        {
            sem.Release();
        }
    }

    private static bool ShouldInspect(HttpRequest request)
    {
        if (!HttpMethods.IsPost(request.Method) && !HttpMethods.IsPut(request.Method))
        {
            return false;
        }

        var path = request.Path;
        if (path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return ApiRateLimitingExtensions.ShouldApplyToPath(path);
    }

    private static async Task<string> ComputeBodySha256HexAsync(Stream body, CancellationToken cancellationToken)
    {
        using var sha = SHA256.Create();
        var buffer = new byte[8192];
        int read;
        while ((read = await body.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)) > 0)
        {
            sha.TransformBlock(buffer, 0, read, null, 0);
        }

        sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
        return Convert.ToHexString(sha.Hash!);
    }

    private static Dictionary<string, string[]> CaptureResponseHeaders(HttpResponse response)
    {
        var dict = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        foreach (var header in response.Headers)
        {
            if (HopByHopHeaderNames.Contains(header.Key))
            {
                continue;
            }

            dict[header.Key] = header.Value.ToArray().Select(static s => s ?? string.Empty).ToArray();
        }

        return dict;
    }

    private static async Task ReplayCachedResponseAsync(HttpContext context, IdempotentResponseSnapshot cached)
    {
        context.Response.Clear();
        context.Response.StatusCode = cached.StatusCode;
        foreach (var (name, values) in cached.Headers)
        {
            if (string.Equals(name, "Content-Length", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            context.Response.Headers[name] = values;
        }

        context.Response.ContentLength = cached.Body.Length;
        await context.Response.Body.WriteAsync(cached.Body, context.RequestAborted);
    }

    private static async Task WriteBadRequestAsync(HttpContext context, string detail)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        context.Response.ContentType = "application/problem+json";
        await JsonSerializer.SerializeAsync(
            context.Response.Body,
            new ValidationProblemDetailsDto(
                Status: StatusCodes.Status400BadRequest,
                Title: "Bad Request",
                Detail: detail,
                Errors: null),
            JsonOptions,
            context.RequestAborted);
    }

    private static async Task WriteConflictAsync(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status409Conflict;
        context.Response.ContentType = "application/problem+json";
        await JsonSerializer.SerializeAsync(
            context.Response.Body,
            new ValidationProblemDetailsDto(
                Status: StatusCodes.Status409Conflict,
                Title: "Conflict",
                Detail: "This Idempotency-Key was already used with a different request payload.",
                Errors: null),
            JsonOptions,
            context.RequestAborted);
    }

    private sealed record IdempotentResponseSnapshot(int StatusCode, Dictionary<string, string[]> Headers, byte[] Body);

    private sealed record ValidationProblemDetailsDto(
        int Status,
        string Title,
        string? Detail,
        Dictionary<string, string[]>? Errors);
}
