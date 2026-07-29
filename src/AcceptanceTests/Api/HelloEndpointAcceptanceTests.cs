using System.Net;
using System.Net.Http.Json;
using ClearMeasure.Bootcamp.AcceptanceTests.TestSupport;
using NUnit.Framework;
using Shouldly;

namespace ClearMeasure.Bootcamp.AcceptanceTests.Api;

[TestFixture]
public class HelloEndpointAcceptanceTests : AcceptanceTestBase
{
    [Test]
    public async Task Should_GetHello_ReturnExpectedResponse()
    {
        // Arrange
        var client = ServerFixture.CreateClient();

        // Act
        var response = await client.GetAsync("/api/hello");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<HelloResponse>();
        result.ShouldNotBeNull();
        result.Message.ShouldBe("Hello, World!");
    }

    [Test]
    public async Task Should_GetHello_WorkWithVersionedRoute()
    {
        // Arrange
        var client = ServerFixture.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/hello");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<HelloResponse>();
        result.ShouldNotBeNull();
        result.Message.ShouldBe("Hello, World!");
    }

    private record HelloResponse(string Message);
}
