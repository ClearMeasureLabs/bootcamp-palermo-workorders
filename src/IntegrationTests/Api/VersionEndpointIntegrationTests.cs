using System.Net;
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
    public async Task Should_Return200AndJson_When_GetUnversioned()
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
        doc.RootElement.TryGetProperty("environment", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("machineName", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("frameworkDescription", out _).ShouldBeTrue();
    }

    [Test]
    public async Task Should_Return200AndSamePayload_When_GetUnversionedAndV1Paths()
    {
        var unversioned = await _client!.GetAsync("/api/version");
        unversioned.StatusCode.ShouldBe(HttpStatusCode.OK);
        var unversionedBody = await unversioned.Content.ReadAsStringAsync();

        var versioned = await _client.GetAsync("/api/v1.0/version");
        versioned.StatusCode.ShouldBe(HttpStatusCode.OK);
        var versionedBody = await versioned.Content.ReadAsStringAsync();

        var unversionedPayload = JsonSerializer.Deserialize<VersionMetadataResponse>(
            unversionedBody,
            ConditionalGetEtag.JsonSerializerOptions);
        var versionedPayload = JsonSerializer.Deserialize<VersionMetadataResponse>(
            versionedBody,
            ConditionalGetEtag.JsonSerializerOptions);

        unversionedPayload.ShouldNotBeNull();
        versionedPayload.ShouldNotBeNull();
        unversionedPayload!.AssemblyVersion.ShouldBe(versionedPayload!.AssemblyVersion);
        unversionedPayload.InformationalVersion.ShouldBe(versionedPayload.InformationalVersion);
        unversionedPayload.Environment.ShouldBe(versionedPayload.Environment);
        unversionedPayload.MachineName.ShouldBe(versionedPayload.MachineName);
        unversionedPayload.FrameworkDescription.ShouldBe(versionedPayload.FrameworkDescription);
    }

    [Test]
    public async Task Should_Return200WithoutApiKey_When_VersionAndMiddlewareEnabled()
    {
        await using var factory = new ApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var unversioned = await client.GetAsync("/api/version");
        unversioned.StatusCode.ShouldBe(HttpStatusCode.OK);

        var versioned = await client.GetAsync("/api/v1.0/version");
        versioned.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task Should_Return304NotModified_When_IfNoneMatchMatchesEtag()
    {
        var first = await _client!.GetAsync("/api/version");
        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        var etag = first.Headers.ETag;
        etag.ShouldNotBeNull();

        using var second = new HttpRequestMessage(HttpMethod.Get, "/api/version");
        second.Headers.IfNoneMatch.Add(etag!);
        var notModified = await _client.SendAsync(second);
        notModified.StatusCode.ShouldBe(HttpStatusCode.NotModified);
        (await notModified.Content.ReadAsByteArrayAsync()).Length.ShouldBe(0);
    }
}
