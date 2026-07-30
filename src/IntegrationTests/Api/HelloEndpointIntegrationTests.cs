using System.Net;
using System.Net.Http.Json;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

[TestFixture]
public class HelloEndpointIntegrationTests : IntegratedTestBase
{
    [Test]
    public async Task GetHello_Should_ReturnOkWithJsonResponse()
    {
        // Arrange
        using var client = TestHost.CreateClient();

        // Act
        var response = await client.GetAsync("/api/hello");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/json");
    }

    [Test]
    public async Task GetHello_Should_ReturnExpectedJsonStructure()
    {
        // Arrange
        using var client = TestHost.CreateClient();

        // Act
        var response = await client.GetAsync("/api/hello");
        var content = await response.Content.ReadFromJsonAsync<HelloResponse>();

        // Assert
        content.ShouldNotBeNull();
        content.Message.ShouldBe("Hello, World!");
    }

    [Test]
    public async Task GetHello_VersionedRoute_Should_ReturnSameResponse()
    {
        // Arrange
        using var client = TestHost.CreateClient();

        // Act
        var unversionedResponse = await client.GetAsync("/api/hello");
        var versionedResponse = await client.GetAsync("/api/v1.0/hello");

        var unversionedContent = await unversionedResponse.Content.ReadFromJsonAsync<HelloResponse>();
        var versionedContent = await versionedResponse.Content.ReadFromJsonAsync<HelloResponse>();

        // Assert
        unversionedResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        versionedResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        unversionedContent!.Message.ShouldBe(versionedContent!.Message);
    }

    [Test]
    public async Task GetHello_Should_AllowAnonymousAccess()
    {
        // Arrange
        using var client = TestHost.CreateClient();

        // Act
        var response = await client.GetAsync("/api/hello");

        // Assert
        response.StatusCode.ShouldNotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.ShouldNotBe(HttpStatusCode.Forbidden);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    private record HelloResponse(string Message);
}