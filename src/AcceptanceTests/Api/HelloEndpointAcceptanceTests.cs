using System.Net;
using System.Text.Json;
using Shouldly;

namespace ClearMeasure.Bootcamp.AcceptanceTests.Api;

[TestFixture]
public class HelloEndpointAcceptanceTests : AcceptanceTestBase
{
    protected override bool RequiresBrowser => false;

    [Test]
    public async Task Api_HelloEndpoint_RespondsWithGreeting()
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
        var body = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(body);
        json.RootElement.GetProperty("message").GetString().ShouldBe("Hello, World!");
    }
}
