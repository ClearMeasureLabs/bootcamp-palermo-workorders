using System.Net;
using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Server;
using Microsoft.AspNetCore.Mvc.Testing;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

[TestFixture]
public class ToolsRandomEndpointIntegrationTests
{
    private WebApplicationFactory<UiServerWebApplicationMarker>? _factory;
    private HttpClient? _client;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _factory = new ToolsRandomWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    [Test]
    public async Task Should_Return200_When_GetToolsRandomUnversioned()
    {
        var response = await _client!.GetAsync("/api/tools/random?kind=int&min=0&max=2");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        doc.RootElement.GetProperty("kind").GetString().ShouldBe("int");
        doc.RootElement.TryGetProperty("value", out _).ShouldBeTrue();
    }

    [Test]
    public async Task Should_Return200_When_GetToolsRandomVersioned()
    {
        var response = await _client!.GetAsync("/api/v1.0/tools/random?kind=guid");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        doc.RootElement.GetProperty("kind").GetString().ShouldBe("guid");
    }

    [Test]
    public async Task Should_Return400_When_GetToolsRandomInvalidKind()
    {
        var response = await _client!.GetAsync("/api/tools/random?kind=invalid");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
