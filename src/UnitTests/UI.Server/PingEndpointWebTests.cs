using System.Net;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class PingEndpointWebTests
{
    [Test]
    public async Task Should_ReturnPongPlainText_When_GetApiPing()
    {
        await using var factory = new ApiVersioningRoutingWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/ping");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("text/plain");
        (await response.Content.ReadAsStringAsync()).Trim().ShouldBe("pong");
    }
}
