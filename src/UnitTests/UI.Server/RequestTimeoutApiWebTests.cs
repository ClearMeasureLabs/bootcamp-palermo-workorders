using System.Net;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class RequestTimeoutApiWebTests
{
    [Test]
    public async Task Should_Return504_When_ApiRequestExceedsConfiguredTimeout()
    {
        await using var factory = new ApiVersioningRoutingWebApplicationFactory();
        factory.ClientOptions.AllowAutoRedirect = false;
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/_test/request-timeout-probe");

        response.StatusCode.ShouldBe(HttpStatusCode.GatewayTimeout);
    }
}
