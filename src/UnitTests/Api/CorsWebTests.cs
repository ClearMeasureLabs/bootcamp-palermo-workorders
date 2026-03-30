using System.Net;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Api;

[TestFixture]
public class CorsWebTests
{
    [Test]
    public async Task Should_ReturnCorsHeadersOnApiPreflight_When_OriginIsAllowed()
    {
        await using var factory = new CorsEnabledApiWebApplicationFactory();
        using var client = factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/version");
        request.Headers.Add("Origin", "https://allowed.example");
        request.Headers.Add("Access-Control-Request-Method", "GET");

        using var response = await client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        response.Headers.TryGetValues("Access-Control-Allow-Origin", out var allowOrigin).ShouldBeTrue();
        allowOrigin!.Single().ShouldBe("https://allowed.example");
    }

    [Test]
    public async Task Should_NotEchoDisallowedOrigin_OnApiPreflight()
    {
        await using var factory = new CorsEnabledApiWebApplicationFactory();
        using var client = factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/version");
        request.Headers.Add("Origin", "https://other.example");
        request.Headers.Add("Access-Control-Request-Method", "GET");

        using var response = await client.SendAsync(request);

        response.Headers.Contains("Access-Control-Allow-Origin").ShouldBeFalse();
    }

    [Test]
    public async Task Should_NotAddCorsHeadersToApiGet_When_CorsDisabled()
    {
        await using var factory = new RateLimitedApiWebApplicationFactory();
        using var client = factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/version");
        request.Headers.Add("Origin", "https://any.example");

        using var response = await client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.Contains("Access-Control-Allow-Origin").ShouldBeFalse();
    }
}
