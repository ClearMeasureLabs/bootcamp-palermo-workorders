using System.Globalization;
using System.Net;
using ClearMeasure.Bootcamp.UI.Server.RateLimiting;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Api;

[TestFixture]
public class ApiRateLimitingWebTests
{
    private static IReadOnlyDictionary<string, string?> StrictLimitSettings(int permitLimit, int windowSeconds = 2) =>
        new Dictionary<string, string?>
        {
            ["ApiRateLimiting:Enabled"] = "true",
            ["ApiRateLimiting:PermitLimit"] = permitLimit.ToString(NumberFormatInfo.InvariantInfo),
            ["ApiRateLimiting:WindowSeconds"] = windowSeconds.ToString(NumberFormatInfo.InvariantInfo),
            ["ApiRateLimiting:SegmentsPerWindow"] = "2",
            ["ApiRateLimiting:QueueLimit"] = "0",
            ["ApiRateLimiting:ApiKeyHeaderName"] = "X-API-Key"
        };

    [Test]
    public async Task Should_Return429WithRetryAfter_When_ApiRequestsExceedSlidingWindowPermitLimit()
    {
        await using var factory = new RateLimitedApiWebApplicationFactory();
        using var client = factory.CreateClient();

        (await client.GetAsync("/api/version")).StatusCode.ShouldBe(HttpStatusCode.OK);

        var limited = await client.GetAsync("/api/version");
        limited.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        limited.Headers.TryGetValues("Retry-After", out var ra).ShouldBeTrue();
        ra!.First().ShouldBe("60");
        limited.Content.Headers.ContentType?.MediaType.ShouldBe("text/plain");
        (await limited.Content.ReadAsStringAsync()).ShouldBe("Too many requests. Please try again later.");
    }

    [Test]
    public async Task Should_NotRateLimit_When_PathIsNotApi()
    {
        await using var factory = new RateLimitedApiWebApplicationFactory();
        using var client = factory.CreateClient();

        for (var i = 0; i < 5; i++)
            (await client.GetAsync("/_healthcheck")).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task RateLimiting_UnderLimit_AllowsRequest()
    {
        await using var factory = new TunableApiRateLimitWebApplicationFactory(StrictLimitSettings(10));
        using var client = factory.CreateClient();
        (await client.GetAsync("/api/time")).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task RateLimiting_ExceedsLimit_Returns429()
    {
        await using var factory = new TunableApiRateLimitWebApplicationFactory(StrictLimitSettings(2));
        using var client = factory.CreateClient();
        (await client.GetAsync("/api/time")).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await client.GetAsync("/api/time")).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await client.GetAsync("/api/time")).StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
    }

    [Test]
    public async Task RateLimiting_ResponseHeaders_IncludeRateLimitInfo()
    {
        await using var factory = new TunableApiRateLimitWebApplicationFactory(StrictLimitSettings(5));
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/time");
        response.Headers.TryGetValues(RateLimitingMiddleware.HeaderLimit, out var limit).ShouldBeTrue();
        response.Headers.TryGetValues(RateLimitingMiddleware.HeaderRemaining, out var remaining).ShouldBeTrue();
        limit!.First().ShouldBe("5");
        int.Parse(remaining!.First(), NumberFormatInfo.InvariantInfo).ShouldBeGreaterThanOrEqualTo(0);
    }

    [Test]
    public async Task RateLimiting_RetryAfterHeader_PresentOn429()
    {
        await using var factory = new TunableApiRateLimitWebApplicationFactory(StrictLimitSettings(1, 5));
        using var client = factory.CreateClient();
        (await client.GetAsync("/api/time")).StatusCode.ShouldBe(HttpStatusCode.OK);
        var blocked = await client.GetAsync("/api/time");
        blocked.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        blocked.Headers.TryGetValues("Retry-After", out var retryValues).ShouldBeTrue();
        retryValues!.First().ShouldBe("5");
    }

    [Test]
    public async Task RateLimiting_DifferentClients_TrackedSeparately()
    {
        await using var factory = new TunableApiRateLimitWebApplicationFactory(StrictLimitSettings(1));
        using var a = factory.CreateClient();
        a.DefaultRequestHeaders.Add("X-API-Key", "client-a");
        using var b = factory.CreateClient();
        b.DefaultRequestHeaders.Add("X-API-Key", "client-b");
        (await a.GetAsync("/api/time")).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await b.GetAsync("/api/time")).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task RateLimiting_BurstRequests_EnforcesLimit()
    {
        await using var factory = new TunableApiRateLimitWebApplicationFactory(StrictLimitSettings(3));
        using var client = factory.CreateClient();
        for (var i = 0; i < 3; i++)
            (await client.GetAsync("/api/time")).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await client.GetAsync("/api/time")).StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
    }

    [Test]
    public async Task RateLimiting_WindowReset_AllowsNewRequests()
    {
        await using var factory = new TunableApiRateLimitWebApplicationFactory(StrictLimitSettings(1, 1));
        using var client = factory.CreateClient();
        (await client.GetAsync("/api/time")).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await client.GetAsync("/api/time")).StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        await Task.Delay(TimeSpan.FromSeconds(1.2));
        (await client.GetAsync("/api/time")).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task RateLimiting_ConfigurationChange_AppliesNewLimits()
    {
        await using var strict = new TunableApiRateLimitWebApplicationFactory(StrictLimitSettings(1));
        await using var loose = new TunableApiRateLimitWebApplicationFactory(StrictLimitSettings(5));
        using (var client = strict.CreateClient())
        {
            (await client.GetAsync("/api/time")).StatusCode.ShouldBe(HttpStatusCode.OK);
            (await client.GetAsync("/api/time")).StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        }

        using (var client = loose.CreateClient())
        {
            for (var i = 0; i < 5; i++)
                (await client.GetAsync("/api/time")).StatusCode.ShouldBe(HttpStatusCode.OK);
            (await client.GetAsync("/api/time")).StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        }
    }

    [Test]
    public async Task RateLimiting_DiagnosticRoute_NotLimited()
    {
        await using var factory = new TunableApiRateLimitWebApplicationFactory(StrictLimitSettings(1));
        using var client = factory.CreateClient();
        (await client.GetAsync("/api/time")).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await client.GetAsync("/api/time")).StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        (await client.PostAsync("/_diagnostics/reset-db-connections", null)).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task RateLimiting_WhenDisabled_AllowsUnlimitedApiCalls()
    {
        var settings = new Dictionary<string, string?>(StrictLimitSettings(1))
        {
            ["ApiRateLimiting:Enabled"] = "false"
        };
        await using var factory = new TunableApiRateLimitWebApplicationFactory(settings);
        using var client = factory.CreateClient();
        for (var i = 0; i < 5; i++)
            (await client.GetAsync("/api/time")).StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
