using System.Net;
using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Shouldly;

namespace ClearMeasure.Bootcamp.AcceptanceTests.Api;

[TestFixture]
public class HelloEndpointAcceptanceTests : AcceptanceTestBase
{
    protected override bool RequiresBrowser => false;

    [Test]
    public async Task HelloEndpoint_Should_ReturnGreeting()
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
        var payload = JsonSerializer.Deserialize<HelloResponse>(
            await response.Content.ReadAsStringAsync(),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        payload.ShouldNotBeNull();
        payload!.Message.ShouldBe("Hello, World!");
    }
}
