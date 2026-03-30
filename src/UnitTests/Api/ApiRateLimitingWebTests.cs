using System.Net;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Api;

[TestFixture]
public class ApiRateLimitingWebTests
{
    [Test]
    public async Task Should_Return429WithRetryAfter_When_ApiRequestsExceedSlidingWindowPermitLimit()
    {
        await using var factory = new RateLimitedApiWebApplicationFactory();
        using var client = factory.CreateClient();

        (await client.GetAsync("/api/version")).StatusCode.ShouldBe(HttpStatusCode.OK);

        var limited = await client.GetAsync("/api/version");
        limited.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        limited.Headers.RetryAfter.ShouldNotBeNull();
    }

    [Test]
    public async Task Should_NotRateLimit_When_PathIsNotApi()
    {
        await using var factory = new RateLimitedApiWebApplicationFactory();
        using var client = factory.CreateClient();

        for (var i = 0; i < 5; i++)
        {
            (await client.GetAsync("/_healthcheck")).StatusCode.ShouldBe(HttpStatusCode.OK);
        }
    }
}
