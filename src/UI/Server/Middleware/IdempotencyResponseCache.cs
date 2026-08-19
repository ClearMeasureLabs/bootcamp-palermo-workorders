using System.Collections.Concurrent;
using ClearMeasure.Bootcamp.UI.Shared;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace ClearMeasure.Bootcamp.UI.Server.Middleware;

internal static class IdempotencyResponseCache
{
    private const string BindingPrefix = "__idempotency_binding:";

    internal static async Task<bool> TryReplayCachedAsync(
        HttpContext context,
        IMemoryCache cache,
        string compositeKey)
    {
        if (!cache.TryGetValue(compositeKey, out IdempotencyMiddleware.IdempotentResponseSnapshot? cachedSnapshot)
            || cachedSnapshot is null)
        {
            return false;
        }

        await IdempotencyMiddleware.ReplayCachedResponseAsync(context, cachedSnapshot);
        return true;
    }

    internal static async Task ProcessUnderLockAsync(
        HttpContext context,
        RequestDelegate next,
        IMemoryCache cache,
        ConcurrentDictionary<string, SemaphoreSlim> keyLocks,
        string idempotencyKey,
        string compositeKey,
        IdempotencyOptions opts)
    {
        var sem = keyLocks.GetOrAdd(idempotencyKey, static _ => new SemaphoreSlim(1, 1));
        await sem.WaitAsync(context.RequestAborted);
        try
        {
            if (await TryReplayCachedAsync(context, cache, compositeKey))
            {
                return;
            }

            if (HasConflictingBinding(cache, idempotencyKey, compositeKey))
            {
                await IdempotencyMiddleware.WriteConflictAsync(context);
                return;
            }

            await ExecuteCaptureAndForwardAsync(context, next, cache, idempotencyKey, compositeKey, opts);
        }
        finally
        {
            sem.Release();
        }
    }

    private static bool HasConflictingBinding(IMemoryCache cache, string idempotencyKey, string compositeKey) =>
        cache.TryGetValue(BindingPrefix + idempotencyKey, out string? boundComposite)
        && !string.Equals(boundComposite, compositeKey, StringComparison.Ordinal);

    private static async Task ExecuteCaptureAndForwardAsync(
        HttpContext context,
        RequestDelegate next,
        IMemoryCache cache,
        string idempotencyKey,
        string compositeKey,
        IdempotencyOptions opts)
    {
        var originalBody = context.Response.Body;
        await using var buffer = new MemoryStream();
        context.Response.Body = buffer;

        try
        {
            await next(context);
        }
        finally
        {
            context.Response.Body = originalBody;
        }

        if (context.Response.StatusCode is >= 200 and < 300)
        {
            CacheSuccessfulResponse(cache, idempotencyKey, compositeKey, context.Response, buffer, opts);
        }

        buffer.Position = 0;
        context.Response.ContentLength = buffer.Length;
        await buffer.CopyToAsync(originalBody, context.RequestAborted);
    }

    private static void CacheSuccessfulResponse(
        IMemoryCache cache,
        string idempotencyKey,
        string compositeKey,
        HttpResponse response,
        MemoryStream buffer,
        IdempotencyOptions opts)
    {
        var bodyBytes = buffer.ToArray();
        var headers = IdempotencyMiddleware.CaptureResponseHeaders(response);
        var snapshot = new IdempotencyMiddleware.IdempotentResponseSnapshot(response.StatusCode, headers, bodyBytes);
        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(Math.Max(1, opts.CacheEntrySeconds))
        };
        cache.Set(compositeKey, snapshot, cacheOptions);
        cache.Set(BindingPrefix + idempotencyKey, compositeKey, cacheOptions);
    }
}
