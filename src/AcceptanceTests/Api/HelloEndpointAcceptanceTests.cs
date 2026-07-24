using System.Net;
using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Shouldly;

namespace ClearMeasure.Bootcamp.AcceptanceTests.Api;

[TestFixture]
public class HelloEndpointAcceptanceTests : AcceptanceTestBase
{
    protected override bool RequiresBrowser => false;

    [Test]
    public async Task Hello_AnonymousRequest_ReturnsOkWithMessage()
    {
        if (!ServerFixture.StartLocalServer)
            Assert.Ignore("Requires local server with HTTP access to /api/*");

        using var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
        using var client = new HttpClient(handler) { BaseAddress = new Uri(ServerFixture.ApplicationBaseUrl) };
        var response = await client.GetAsync("/api/hello");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/json");
        var json = await response.Content.ReadAsStringAsync();
        var payload = JsonSerializer.Deserialize<HelloResponse>(json, ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload!.Message.ShouldBe("Hello, World!");
    }

    [Test]
    public async Task Hello_RateLimitingApplies()
    {
        if (!ServerFixture.StartLocalServer)
            Assert.Ignore("Requires local server with HTTP access to /api/*");

        using var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
        using var client = new HttpClient(handler) { BaseAddress = new Uri(ServerFixture.ApplicationBaseUrl) };
        var response = await client.GetAsync("/api/hello");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        if (!response.Headers.TryGetValues("X-RateLimit-Limit", out _))
            Assert.Ignore("API rate limiting is disabled in this environment (e.g. Development appsettings).");

        response.Headers.TryGetValues("X-RateLimit-Remaining", out _).ShouldBeTrue();
        response.Headers.TryGetValues("X-RateLimit-Reset", out _).ShouldBeTrue();
    }
}
