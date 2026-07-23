using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

public class TimeControllerTests : IClassFixture<WebApplicationFactory<UiServerWebApplicationMarker>>
{
    private readonly WebApplicationFactory<UiServerWebApplicationMarker> _factory;

    public TimeControllerTests(WebApplicationFactory<UiServerWebApplicationMarker> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Get_ReturnsUtcTimeInIso8601Format()
    {
        // Arrange
        var client = _factory.CreateClient();
        var beforeRequest = DateTime.UtcNow;

        // Act
        var response = await client.GetAsync("/api/time");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        content.TryGetProperty("utc", out var utcProperty).Should().BeTrue();
        
        var utcString = utcProperty.GetString();
        utcString.Should().NotBeNullOrEmpty();
        
        // Verify it's valid ISO-8601 format
        var parsedTime = DateTime.Parse(utcString!);
        parsedTime.Kind.Should().Be(DateTimeKind.Utc);
        
        // Verify the time is reasonable (within a few seconds of now)
        var afterRequest = DateTime.UtcNow;
        parsedTime.Should().BeOnOrAfter(beforeRequest.AddSeconds(-5));
        parsedTime.Should().BeOnOrBefore(afterRequest.AddSeconds(5));
    }

    [Fact]
    public async Task Get_ReturnsJsonContentType()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/time");

        // Assert
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }
}
