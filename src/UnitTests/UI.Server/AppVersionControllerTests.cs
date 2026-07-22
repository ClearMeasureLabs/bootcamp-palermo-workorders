using System.Net;
using System.Net.Http.Json;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class AppVersionControllerTests
{
    private ApiVersioningRoutingWebApplicationFactory? _factory;
    private HttpClient? _client;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _factory = new ApiVersioningRoutingWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    [Test]
    public async Task Should_ReturnVersionJson_When_GetAppVersion()
    {
        var response = await _client!.GetAsync("/api/appversion");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<AppVersionResponse>();
        content.ShouldNotBeNull();
        content!.Version.ShouldNotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Should_ReturnVersionJson_When_GetAppVersion_VersionedRoute()
    {
        var response = await _client!.GetAsync("/api/v1.0/appversion");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<AppVersionResponse>();
        content.ShouldNotBeNull();
        content!.Version.ShouldNotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Should_ReturnCacheHeaders_When_GetAppVersion()
    {
        var response = await _client!.GetAsync("/api/appversion");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.CacheControl.ShouldNotBeNull();
    }

    private record AppVersionResponse(string Version);
}
