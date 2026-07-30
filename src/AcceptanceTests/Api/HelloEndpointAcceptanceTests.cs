using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace ClearMeasure.Bootcamp.AcceptanceTests.Api;

public class HelloEndpointAcceptanceTests : AcceptanceTestBase
{
    public HelloEndpointAcceptanceTests(ServerFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task HelloEndpoint_Should_BeAccessibleAndReturnGreeting()
    {
        // Act
        var response = await Client.GetAsync("/api/hello");
        var content = await response.Content.ReadFromJsonAsync<HelloResponse>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().NotBeNull();
        content!.Message.Should().Be("Hello, World!");
    }

    private record HelloResponse(string Message);
}
