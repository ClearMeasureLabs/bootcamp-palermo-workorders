using System.Net;
using System.Net.Http.Json;
using ClearMeasure.Bootcamp.UI.Server;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

public class AppVersionControllerTests : IClassFixture<WebApplicationFactory<UiServerWebApplicationMarker>>
{
    private readonly WebApplicationFactory<UiServerWebApplicationMarker> _factory;

    public AppVersionControllerTests(WebApplicationFactory<UiServerWebApplicationMarker> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetAppVersion_ReturnsVersionJson()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/appversion");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<AppVersionResponse>();
        content.Should().NotBeNull();
        content!.Version.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GetAppVersion_VersionedRoute_ReturnsVersionJson()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1.0/appversion");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<AppVersionResponse>();
        content.Should().NotBeNull();
        content!.Version.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GetAppVersion_ReturnsOutputCacheHeaders()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/appversion");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.CacheControl.Should().NotBeNull();
    }

    private record AppVersionResponse(string Version);
}
