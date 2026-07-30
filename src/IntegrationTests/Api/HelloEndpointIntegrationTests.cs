using System.Net;
using System.Net.Http.Json;
using ClearMeasure.Bootcamp.UnitTests.UI.Server;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

[TestFixture]
public class HelloEndpointIntegrationTests
{
    private DiagnosticsWebApplicationFactory? _factory;
    private HttpClient? _client;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _factory = new DiagnosticsWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    [Test]
    public async Task GetHello_Should_ReturnOkWithJsonResponse()
    {
        // Act
        var response = await _client!.GetAsync("/api/hello");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/json");
    }

    [Test]
    public async Task GetHello_Should_ReturnExpectedJsonStructure()
    {
        // Act
        var response = await _client!.GetAsync("/api/hello");
        var content = await response.Content.ReadFromJsonAsync<HelloResponse>();

        // Assert
        content.ShouldNotBeNull();
        content.Message.ShouldBe("Hello, World!");
    }

    [Test]
    public async Task GetHello_VersionedRoute_Should_ReturnSameResponse()
    {
        // Act
        var unversionedResponse = await _client!.GetAsync("/api/hello");
        var versionedResponse = await _client!.GetAsync("/api/v1.0/hello");

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
        // Act
        var response = await _client!.GetAsync("/api/hello");

        // Assert
        response.StatusCode.ShouldNotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.ShouldNotBe(HttpStatusCode.Forbidden);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    private record HelloResponse(string Message);
}
