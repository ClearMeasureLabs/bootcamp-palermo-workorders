using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using ClearMeasure.Bootcamp.UnitTests.UI.Server;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

[TestFixture]
public class VersionEndpointIntegrationTests
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
    public async Task GetApiVersion_Returns200_WithAllRequiredProperties()
    {
        var response = await _client!.GetAsync("/api/version");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        doc.RootElement.TryGetProperty("assemblyVersion", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("informationalVersion", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("buildConfiguration", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("environment", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("machineName", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("frameworkDescription", out _).ShouldBeTrue();
    }

    [Test]
    public async Task GetVersionedApiVersion_Returns200_WithAllRequiredProperties()
    {
        var response = await _client!.GetAsync("/api/v1.0/version");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        doc.RootElement.TryGetProperty("assemblyVersion", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("informationalVersion", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("buildConfiguration", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("environment", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("machineName", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("frameworkDescription", out _).ShouldBeTrue();
    }

    [Test]
    public async Task GetApiVersion_AllowsAnonymousAccess_NoApiKeyRequired()
    {
        await using var factory = new ApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var unversioned = await client.GetAsync("/api/version");
        unversioned.StatusCode.ShouldBe(HttpStatusCode.OK);

        var versioned = await client.GetAsync("/api/v1.0/version");
        versioned.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task GetApiVersion_ReturnsWeakETag_AndSupports304()
    {
        var first = await _client!.GetAsync("/api/version");
        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        var etag = first.Headers.ETag;
        etag.ShouldNotBeNull();

        using var second = new HttpRequestMessage(HttpMethod.Get, "/api/version");
        second.Headers.IfNoneMatch.Add(etag);
        var notModified = await _client.SendAsync(second);
        notModified.StatusCode.ShouldBe(HttpStatusCode.NotModified);
        (await notModified.Content.ReadAsByteArrayAsync()).Length.ShouldBe(0);
    }

    [Test]
    public async Task GetApiVersion_CachesResponse_ForConfiguredPeriod()
    {
        using var first = await _client!.GetAsync("/api/version");
        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        first.Headers.Age.ShouldBeNull();

        using var second = await _client.GetAsync("/api/version");
        second.StatusCode.ShouldBe(HttpStatusCode.OK);
        second.Headers.Age.ShouldNotBeNull();
        second.Headers.Age!.Value.TotalSeconds.ShouldBeGreaterThanOrEqualTo(0);
    }

    [Test]
    public async Task GetApiVersion_ExposesEnvironment_FromHost()
    {
        var response = await _client!.GetAsync("/api/version");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<VersionMetadataResponse>(
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload!.Environment.ShouldBe("Testing");
        payload.BuildConfiguration.ShouldNotBeNullOrEmpty();
    }
}
