using System.Collections.Concurrent;
using System.Text;
using ClearMeasure.Bootcamp.UI.Server.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class IdempotencyMiddlewareEnabledTests
{
    [Test]
    public async Task HandleAsync_WhenDisabled_InvokesNextTwice_ForSameKey()
    {
        var callCount = 0;
        RequestDelegate next = _ =>
        {
            Interlocked.Increment(ref callCount);
            return Task.CompletedTask;
        };

        var cache = new MemoryCache(new MemoryCacheOptions());
        var keyLocks = new ConcurrentDictionary<string, SemaphoreSlim>();
        var monitor = new FixedOptionsMonitor(new IdempotencyOptions { Enabled = false });

        await IdempotencyMiddlewareHandler.HandleAsync(CreateContext(), next, cache, keyLocks, monitor);
        await IdempotencyMiddlewareHandler.HandleAsync(CreateContext(), next, cache, keyLocks, monitor);

        callCount.ShouldBe(2);
    }

    [Test]
    public async Task HandleAsync_WhenEnabled_InvokesNext_ForNonInspectedRequest()
    {
        var callCount = 0;
        RequestDelegate next = _ =>
        {
            Interlocked.Increment(ref callCount);
            return Task.CompletedTask;
        };

        var cache = new MemoryCache(new MemoryCacheOptions());
        var keyLocks = new ConcurrentDictionary<string, SemaphoreSlim>();
        var monitor = new FixedOptionsMonitor(new IdempotencyOptions { Enabled = true });
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = "/";

        await IdempotencyMiddlewareHandler.HandleAsync(context, next, cache, keyLocks, monitor);

        callCount.ShouldBe(1);
    }

    private static DefaultHttpContext CreateContext()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = "/api/tools/hash";
        context.Request.Headers["Idempotency-Key"] = "same-key";
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("{}"));
        return context;
    }

    private sealed class FixedOptionsMonitor : IOptionsMonitor<IdempotencyOptions>
    {
        public FixedOptionsMonitor(IdempotencyOptions currentValue) => CurrentValue = currentValue;

        public IdempotencyOptions CurrentValue { get; }

        public IdempotencyOptions Get(string? name) => CurrentValue;

        public IDisposable OnChange(Action<IdempotencyOptions, string?> listener) =>
            EmptyDisposable.Instance;
    }

    private sealed class EmptyDisposable : IDisposable
    {
        public static readonly EmptyDisposable Instance = new();

        public void Dispose()
        {
        }
    }
}
