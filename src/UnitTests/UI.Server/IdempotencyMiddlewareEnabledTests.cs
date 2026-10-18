using System.Collections.Concurrent;
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

        Task Next(HttpContext _)
        {
            Interlocked.Increment(ref callCount);
            return Task.CompletedTask;
        }

        var cache = new MemoryCache(new MemoryCacheOptions());
        var keyLocks = new ConcurrentDictionary<string, SemaphoreSlim>();
        var monitor = new FixedOptionsMonitor(new IdempotencyOptions { Enabled = false });

        await IdempotencyMiddlewareHandler.HandleAsync(CreateContext(), Next, cache, keyLocks, monitor);
        await IdempotencyMiddlewareHandler.HandleAsync(CreateContext(), Next, cache, keyLocks, monitor);

        callCount.ShouldBe(2);
    }

    [Test]
    public async Task HandleAsync_WhenEnabled_InvokesNext_ForNonInspectedRequest()
    {
        var callCount = 0;

        Task Next(HttpContext _)
        {
            Interlocked.Increment(ref callCount);
            return Task.CompletedTask;
        }

        var cache = new MemoryCache(new MemoryCacheOptions());
        var keyLocks = new ConcurrentDictionary<string, SemaphoreSlim>();
        var monitor = new FixedOptionsMonitor(new IdempotencyOptions { Enabled = true });
        var context = new DefaultHttpContext
        {
            Request =
            {
                Method = HttpMethods.Get,
                Path = "/"
            }
        };

        await IdempotencyMiddlewareHandler.HandleAsync(context, Next, cache, keyLocks, monitor);

        callCount.ShouldBe(1);
    }

    private static DefaultHttpContext CreateContext()
    {
        var context = new DefaultHttpContext
        {
            Request =
            {
                Method = HttpMethods.Post,
                Path = "/api/tools/hash",
                Body = new MemoryStream("{}"u8.ToArray())
            }
        };
        context.Request.Headers["Idempotency-Key"] = "same-key";
        return context;
    }

    private sealed class FixedOptionsMonitor(IdempotencyOptions currentValue) : IOptionsMonitor<IdempotencyOptions>
    {
        public IdempotencyOptions CurrentValue => currentValue;

        public IdempotencyOptions Get(string? name) => currentValue;

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
