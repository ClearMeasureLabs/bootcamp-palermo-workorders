using System.Net;
using System.Net.Http.Json;
using Shouldly;

namespace ClearMeasure.Bootcamp.AcceptanceTests.Api;

[TestFixture]
public class HelloEndpointAcceptanceTests : AcceptanceTestBase
{
    protected override bool RequiresBrowser => false;

    [Test]
    public async Task HelloEndpoint_Should_BeAccessibleAndReturnGreeting()
    {
        // Arrange
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
        using var client = new HttpClient(handler) { BaseAddress = new Uri(ServerFixture.ApplicationBaseUrl) };

        // Act
        var response = await client.GetAsync("/api/hello");
        var content = await response.Content.ReadFromJsonAsync<HelloResponse>();

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        content.ShouldNotBeNull();
        content.Message.ShouldBe("Hello, World!");
    }

    private record HelloResponse(string Message);
}