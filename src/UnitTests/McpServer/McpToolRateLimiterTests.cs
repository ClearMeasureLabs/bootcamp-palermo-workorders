using System.Collections.Concurrent;
using ClearMeasure.Bootcamp.McpServer.RateLimiting;
using Microsoft.Extensions.Options;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.McpServer;

[TestFixture]
public class McpToolRateLimiterTests
{
    [Test]
    public void TryAcquire_WhenBucketHasBurstCapacity_AllowsBurstThenRejects()
    {
        var timeProvider = new StubTimeProvider();
        var limiter = CreateLimiter(timeProvider, capacity: 2, refillTokens: 1, refillPeriodSeconds: 10);

        limiter.TryAcquire("client-1", "list-work-orders", out _).ShouldBeTrue();
        limiter.TryAcquire("client-1", "list-work-orders", out _).ShouldBeTrue();
        limiter.TryAcquire("client-1", "list-work-orders", out var retryAfter).ShouldBeFalse();

        retryAfter.ShouldBe(TimeSpan.FromSeconds(10));
        timeProvider.Advance(TimeSpan.FromSeconds(10));
        limiter.TryAcquire("client-1", "list-work-orders", out _).ShouldBeTrue();
    }

    [Test]
    public void TryAcquire_WhenLimitsDifferByToolAndSession_UsesSeparateBuckets()
    {
        var timeProvider = new StubTimeProvider();
        var options = CreateOptions(capacity: 1, refillTokens: 1, refillPeriodSeconds: 60);
        options.Tools["save-work-order"] = new McpToolRateLimitOptions
        {
            Capacity = 2,
            RefillTokens = 1,
            RefillPeriodSeconds = 60
        };
        var limiter = new McpToolRateLimiter(Options.Create(options), timeProvider);

        limiter.TryAcquire("client-1", "list-work-orders", out _).ShouldBeTrue();
        limiter.TryAcquire("client-1", "list-work-orders", out _).ShouldBeFalse();
        limiter.TryAcquire("client-2", "list-work-orders", out _).ShouldBeTrue();
        limiter.TryAcquire("client-1", "save-work-order", out _).ShouldBeTrue();
        limiter.TryAcquire("client-1", "save-work-order", out _).ShouldBeTrue();
        limiter.TryAcquire("client-1", "save-work-order", out _).ShouldBeFalse();
    }

    [Test]
    public void TryAcquire_WhenTrackingCapacityIsReached_EvictsIdleBuckets()
    {
        var timeProvider = new StubTimeProvider();
        var options = CreateOptions(capacity: 1, refillTokens: 1, refillPeriodSeconds: 60);
        options.MaxTrackedBuckets = 1;
        options.IdleTimeoutMinutes = 1;
        var limiter = new McpToolRateLimiter(Options.Create(options), timeProvider);

        limiter.TryAcquire("client-1", "list-work-orders", out _).ShouldBeTrue();
        limiter.TryAcquire("client-2", "list-work-orders", out _).ShouldBeFalse();

        timeProvider.Advance(TimeSpan.FromMinutes(1).Add(TimeSpan.FromSeconds(1)));

        limiter.TryAcquire("client-2", "list-work-orders", out _).ShouldBeTrue();
    }

    [Test]
    public void TryAcquire_WhenOneHundredClientsExceedTheirLimit_RejectsTheExcessCalls()
    {
        var limiter = CreateLimiter(new StubTimeProvider(), capacity: 1, refillTokens: 1, refillPeriodSeconds: 60);
        var results = new ConcurrentBag<bool>();

        Parallel.For(0, 100, client =>
        {
            var clientSessionId = $"client-{client}";
            results.Add(limiter.TryAcquire(clientSessionId, "list-work-orders", out _));
            results.Add(limiter.TryAcquire(clientSessionId, "list-work-orders", out _));
        });

        results.Count.ShouldBe(200);
        results.Count(allowed => allowed).ShouldBe(100);
    }

    private static McpToolRateLimiter CreateLimiter(
        StubTimeProvider timeProvider,
        int capacity,
        int refillTokens,
        int refillPeriodSeconds) =>
        new(Options.Create(CreateOptions(capacity, refillTokens, refillPeriodSeconds)), timeProvider);

    private static McpToolRateLimitingOptions CreateOptions(
        int capacity,
        int refillTokens,
        int refillPeriodSeconds) =>
        new()
        {
            Default = new McpToolRateLimitOptions
            {
                Capacity = capacity,
                RefillTokens = refillTokens,
                RefillPeriodSeconds = refillPeriodSeconds
            }
        };

    private sealed class StubTimeProvider : TimeProvider
    {
        private DateTimeOffset _now = new(2026, 11, 12, 0, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan amount) => _now = _now.Add(amount);
    }
}
