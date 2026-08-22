using System.Net;
using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UnitTests.UI.Server;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

[TestFixture]
public class TimestampConverterEndpointIntegrationTests
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
    public async Task Should_Return200AndJsonShape_When_GetUnversionedWithEpoch()
    {
        var response = await _client!.GetAsync("/api/tools/timestamp-converter?epoch=1704067200");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");
        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        root.TryGetProperty("unixSeconds", out _).ShouldBeTrue();
        root.TryGetProperty("unixMilliseconds", out _).ShouldBeTrue();
        root.TryGetProperty("iso8601Utc", out _).ShouldBeTrue();
        root.TryGetProperty("utcDisplay", out _).ShouldBeTrue();
        root.TryGetProperty("rfc1123", out _).ShouldBeTrue();
        root.GetProperty("unixSeconds").GetInt64().ShouldBe(1704067200);
    }

    [Test]
    public async Task Should_Return200AndJsonShape_When_GetVersionedWithIso()
    {
        var response = await _client!.GetAsync("/api/v1.0/tools/timestamp-converter?iso=2024-01-01T00:00:00Z");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");
        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        root.TryGetProperty("unixSeconds", out _).ShouldBeTrue();
        root.TryGetProperty("unixMilliseconds", out _).ShouldBeTrue();
        root.TryGetProperty("iso8601Utc", out _).ShouldBeTrue();
        root.TryGetProperty("utcDisplay", out _).ShouldBeTrue();
        root.TryGetProperty("rfc1123", out _).ShouldBeTrue();
        root.GetProperty("unixSeconds").GetInt64().ShouldBe(1704067200);
    }

    [Test]
    public async Task Should_Return400_When_NoQueryParameters()
    {
        var response = await _client!.GetAsync("/api/tools/timestamp-converter");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Should_Return200WithoutApiKey_When_MiddlewareEnabled()
    {
        await using var factory = new ApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var unversioned = await client.GetAsync("/api/tools/timestamp-converter?epoch=1704067200");
        unversioned.StatusCode.ShouldBe(HttpStatusCode.OK);

        var versioned = await client.GetAsync("/api/v1.0/tools/timestamp-converter?iso=2024-01-01T00:00:00Z");
        versioned.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
