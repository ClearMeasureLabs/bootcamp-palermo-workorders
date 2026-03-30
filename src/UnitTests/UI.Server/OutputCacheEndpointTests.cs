using System.Net;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class OutputCacheEndpointTests
{
    [Test]
    public async Task Should_ServeVersionFromOutputCache_When_SecondRequestWithinPolicyWindow()
    {
        await using var factory = new ApiVersioningRoutingWebApplicationFactory();
        using var client = factory.CreateClient();

        using var first = await client.GetAsync("/api/version");
        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        first.Headers.Age.ShouldBeNull();

        using var second = await client.GetAsync("/api/version");
        second.StatusCode.ShouldBe(HttpStatusCode.OK);
        second.Headers.Age.ShouldNotBeNull();
        second.Headers.Age!.Value.TotalSeconds.ShouldBeGreaterThanOrEqualTo(0);
    }

    [Test]
    public async Task Should_NotOutputCache_When_GetTimeRepeated()
    {
        await using var factory = new ApiVersioningRoutingWebApplicationFactory();
        using var client = factory.CreateClient();

        using var first = await client.GetAsync("/api/time");
        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        first.Headers.Age.ShouldBeNull();

        using var second = await client.GetAsync("/api/time");
        second.StatusCode.ShouldBe(HttpStatusCode.OK);
        second.Headers.Age.ShouldBeNull();
    }

    [Test]
    public async Task Should_NotOutputCache_When_GetHealthRepeated()
    {
        await using var factory = new ApiVersioningRoutingWebApplicationFactory();
        using var client = factory.CreateClient();

        using var first = await client.GetAsync("/api/health");
        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        first.Headers.Age.ShouldBeNull();

        using var second = await client.GetAsync("/api/health");
        second.StatusCode.ShouldBe(HttpStatusCode.OK);
        second.Headers.Age.ShouldBeNull();
    }

    [Test]
    public async Task Should_NotOutputCache_When_GetDetailedHealthRepeated()
    {
        await using var factory = new ApiVersioningRoutingWebApplicationFactory();
        using var client = factory.CreateClient();

        using var first = await client.GetAsync("/api/health/detailed");
        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        first.Headers.Age.ShouldBeNull();

        using var second = await client.GetAsync("/api/health/detailed");
        second.StatusCode.ShouldBe(HttpStatusCode.OK);
        second.Headers.Age.ShouldBeNull();
    }
}
