using System.Net;
using System.Net.Http.Json;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UnitTests.UI.Server;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

[TestFixture]
public class EchoEndpointIntegrationTests
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
        var response = await _client!.GetAsync("/api/echo?probe=1");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");
        var payload = await response.Content.ReadFromJsonAsync<EchoResponse>(
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload!.Method.ShouldBe(HttpMethod.Get.Method);
        payload.Path.ShouldContain("echo");
        payload.QueryString.ShouldContain("probe=1");
        payload.Query["probe"].ShouldBe("1");
        payload.Scheme.ShouldNotBeNullOrWhiteSpace();
        payload.Host.ShouldNotBeNullOrWhiteSpace();
        payload.Protocol.ShouldNotBeNullOrWhiteSpace();
        payload.Headers.Count.ShouldBeGreaterThan(0);
    }

    [Test]
    public async Task Should_Return200AndJson_When_GetVersioned()
    {
        var response = await _client!.GetAsync("/api/v1.0/echo?probe=2");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");
        var payload = await response.Content.ReadFromJsonAsync<EchoResponse>(
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload!.Method.ShouldBe(HttpMethod.Get.Method);
        payload.Path.ShouldContain("echo");
        payload.Query["probe"].ShouldBe("2");
    }

    [Test]
    public async Task Should_Return200WithoutApiKey_When_EchoAndMiddlewareEnabled()
    {
        await using var factory = new ApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var unversioned = await client.GetAsync("/api/echo");
        unversioned.StatusCode.ShouldBe(HttpStatusCode.OK);
        var unversionedMedia = unversioned.Content.Headers.ContentType?.MediaType;
        unversionedMedia.ShouldNotBeNull();
        unversionedMedia!.ShouldContain("application/json");

        var versioned = await client.GetAsync("/api/v1.0/echo");
        versioned.StatusCode.ShouldBe(HttpStatusCode.OK);
        var versionedMedia = versioned.Content.Headers.ContentType?.MediaType;
        versionedMedia.ShouldNotBeNull();
        versionedMedia!.ShouldContain("application/json");
    }
}
