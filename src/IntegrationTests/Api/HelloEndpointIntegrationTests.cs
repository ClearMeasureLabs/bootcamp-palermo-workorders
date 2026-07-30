using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

public class HelloEndpointIntegrationTests : IntegrationTestBase
{
    public HelloEndpointIntegrationTests(IntegrationTestFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task GetHello_Should_ReturnOkWithJsonResponse()
    {
        // Act
        var response = await Client.GetAsync("/api/hello");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task GetHello_Should_ReturnExpectedJsonStructure()
    {
        // Act
        var response = await Client.GetAsync("/api/hello");
        var content = await response.Content.ReadFromJsonAsync<HelloResponse>();

        // Assert
        content.Should().NotBeNull();
        content!.Message.Should().Be("Hello, World!");
    }

    [Fact]
    public async Task GetHello_VersionedRoute_Should_ReturnSameResponse()
    {
        // Act
        var unversionedResponse = await Client.GetAsync("/api/hello");
        var versionedResponse = await Client.GetAsync("/api/v1.0/hello");

        var unversionedContent = await unversionedResponse.Content.ReadFromJsonAsync<HelloResponse>();
        var versionedContent = await versionedResponse.Content.ReadFromJsonAsync<HelloResponse>();

        // Assert
        unversionedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        versionedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        unversionedContent!.Message.Should().Be(versionedContent!.Message);
    }

    [Fact]
    public async Task GetHello_Should_AllowAnonymousAccess()
    {
        // Act
        var response = await Client.GetAsync("/api/hello");

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private record HelloResponse(string Message);
}
