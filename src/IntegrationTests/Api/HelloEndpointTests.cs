using System.Net;
using System.Net.Http.Json;
using ClearMeasure.Bootcamp.IntegrationTests.TestSupport;
using NUnit.Framework;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

[TestFixture]
public class HelloEndpointTests : IntegratedTestBase
{
    [Test]
    public async Task Should_GetHello_ReturnOkWithMessage()
    {
        // Arrange
        var client = TestHost.CreateClient();

        // Act
        var response = await client.GetAsync("/api/hello");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<HelloResponse>();
        result.ShouldNotBeNull();
        result.Message.ShouldBe("Hello, World!");
    }

    [Test]
    public async Task Should_GetHello_ReturnJsonContentType()
    {
        // Arrange
        var client = TestHost.CreateClient();

        // Act
        var response = await client.GetAsync("/api/hello");

        // Assert
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/json");
    }

    private record HelloResponse(string Message);
}
