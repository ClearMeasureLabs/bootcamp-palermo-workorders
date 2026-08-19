using ClearMeasure.Bootcamp.UI.Server;
using ClearMeasure.Bootcamp.UI.Server.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Shouldly;
using System.Threading.RateLimiting;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class RateLimitingMiddlewareTests
{
    [Test]
    public async Task InvokeAsync_CallsNext_WhenEndpointHasNoRateLimitAttribute()
    {
        var called = false;
        RequestDelegate next = _ =>
        {
            called = true;
            return Task.CompletedTask;
        };

        var monitor = new StubOptionsMonitor<ApiRateLimitingOptions>(new ApiRateLimitingOptions { Enabled = true });
        var limiter = PartitionedRateLimiter.Create<HttpContext, string>(_ =>
            RateLimitPartition.GetNoLimiter(string.Empty));

        var middleware = new RateLimitingMiddleware(next, monitor, limiter);
        var context = new DefaultHttpContext();
        await middleware.InvokeAsync(context);

        called.ShouldBeTrue();
    }

    [Test]
    public void ShouldApply_ReturnsFalse_WhenEndpointMissing()
    {
        RateLimitingMiddlewareRules.ShouldApply(new DefaultHttpContext()).ShouldBeFalse();
    }

    private sealed class StubOptionsMonitor<T>(T value) : IOptionsMonitor<T> where T : class
    {
        public T CurrentValue => value;

        public T Get(string? name) => value;

        public IDisposable OnChange(Action<T, string?> listener) => new NoOpDisposable();

        private sealed class NoOpDisposable : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }
}
