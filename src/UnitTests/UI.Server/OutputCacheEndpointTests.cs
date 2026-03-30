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
}
