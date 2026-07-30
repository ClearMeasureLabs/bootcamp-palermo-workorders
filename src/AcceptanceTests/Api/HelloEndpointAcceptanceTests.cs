using System.Net;
using System.Net.Http.Json;
using Shouldly;

namespace ClearMeasure.Bootcamp.AcceptanceTests.Api;

[TestFixture]
public class HelloEndpointAcceptanceTests : AcceptanceTestBase
{
    [Test]
    public async Task HelloEndpoint_Should_BeAccessibleAndReturnGreeting()
    {
        // Act
        var response = await Client.GetAsync("/api/hello");
        var content = await response.Content.ReadFromJsonAsync<HelloResponse>();

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        content.ShouldNotBeNull();
        content.Message.ShouldBe("Hello, World!");
    }

    private record HelloResponse(string Message);
}