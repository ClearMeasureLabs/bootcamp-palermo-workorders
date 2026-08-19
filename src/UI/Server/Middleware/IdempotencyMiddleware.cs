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

        var idempotencyKey = TryReadIdempotencyKey(context.Request);
        if (idempotencyKey is null)
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

        var compositeKey = await BuildCompositeKeyAsync(context.Request, idempotencyKey, context.RequestAborted);
        if (await TryReplayCachedAsync(context, compositeKey))
        {
            return;
        }

        await ProcessUnderLockAsync(context, idempotencyKey, compositeKey, opts);
    }

    internal static bool ShouldInspect(HttpRequest request)
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

    internal static string? TryReadIdempotencyKey(HttpRequest request)
    {
        if (!request.Headers.TryGetValue(IdempotencyConstants.HeaderName, out var keyValues))
        {
            return null;
        }

        var trimmed = keyValues.ToString().Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }

    internal static async Task<string> BuildCompositeKeyAsync(
        HttpRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        request.EnableBuffering();
        request.Body.Position = 0;
        var bodyHash = await ComputeBodySha256HexAsync(request.Body, cancellationToken);
        request.Body.Position = 0;
        return $"{request.Method}\u001f{request.Path.Value}\u001f{idempotencyKey}\u001f{bodyHash}";
    }

    private async Task<bool> TryReplayCachedAsync(HttpContext context, string compositeKey)
    {
        if (!_cache.TryGetValue(compositeKey, out IdempotentResponseSnapshot? cachedSnapshot)
            || cachedSnapshot is null)
        {
            return false;
        }

        await ReplayCachedResponseAsync(context, cachedSnapshot);
        return true;
    }

    private async Task ProcessUnderLockAsync(
        HttpContext context,
        string idempotencyKey,
        string compositeKey,
        IdempotencyOptions opts)
    {
        var sem = _keyLocks.GetOrAdd(idempotencyKey, static _ => new SemaphoreSlim(1, 1));
        await sem.WaitAsync(context.RequestAborted);
        try
        {
            if (await TryReplayCachedAsync(context, compositeKey))
            {
                return;
            }

            if (HasConflictingBinding(idempotencyKey, compositeKey))
            {
                await WriteConflictAsync(context);
                return;
            }

            await ExecuteCaptureAndForwardAsync(context, idempotencyKey, compositeKey, opts);
        }
        finally
        {
            sem.Release();
        }
    }

    private bool HasConflictingBinding(string idempotencyKey, string compositeKey) =>
        _cache.TryGetValue(BindingPrefix + idempotencyKey, out string? boundComposite)
        && !string.Equals(boundComposite, compositeKey, StringComparison.Ordinal);

    private async Task ExecuteCaptureAndForwardAsync(
        HttpContext context,
        string idempotencyKey,
        string compositeKey,
        IdempotencyOptions opts)
    {
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
            CacheSuccessfulResponse(idempotencyKey, compositeKey, context.Response, buffer, opts);
        }

        buffer.Position = 0;
        context.Response.ContentLength = buffer.Length;
        await buffer.CopyToAsync(originalBody, context.RequestAborted);
    }

    private void CacheSuccessfulResponse(
        string idempotencyKey,
        string compositeKey,
        HttpResponse response,
        MemoryStream buffer,
        IdempotencyOptions opts)
    {
        var bodyBytes = buffer.ToArray();
        var headers = CaptureResponseHeaders(response);
        var snapshot = new IdempotentResponseSnapshot(response.StatusCode, headers, bodyBytes);
        var cacheSeconds = Math.Max(1, opts.CacheEntrySeconds);
        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(cacheSeconds)
        };
        _cache.Set(compositeKey, snapshot, cacheOptions);
        _cache.Set(BindingPrefix + idempotencyKey, compositeKey, cacheOptions);
    }

    internal static async Task<string> ComputeBodySha256HexAsync(Stream body, CancellationToken cancellationToken)
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

    internal static Dictionary<string, string[]> CaptureResponseHeaders(HttpResponse response)
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

    internal static async Task ReplayCachedResponseAsync(HttpContext context, IdempotentResponseSnapshot cached)
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

    internal static async Task WriteBadRequestAsync(HttpContext context, string detail)
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

    internal static async Task WriteConflictAsync(HttpContext context)
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

    internal sealed record IdempotentResponseSnapshot(int StatusCode, Dictionary<string, string[]> Headers, byte[] Body);

    private sealed record ValidationProblemDetailsDto(
        int Status,
        string Title,
        string? Detail,
        Dictionary<string, string[]>? Errors);
}
