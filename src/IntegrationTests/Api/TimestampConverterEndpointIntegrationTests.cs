using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
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
    public async Task Should_Return200WithJsonShape_When_GetUnversionedWithEpochSeconds()
    {
        var response = await _client!.GetAsync("/api/tools/timestamp-converter?epoch=1609459200");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        await AssertJsonShape(response);
    }

    [Test]
    public async Task Should_Return200WithJsonShape_When_GetVersionedWithEpochSeconds()
    {
        var response = await _client!.GetAsync("/api/v1.0/tools/timestamp-converter?epoch=1609459200");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        await AssertJsonShape(response);
    }

    [Test]
    public async Task Should_Return200WithJsonShape_When_GetWithIso8601()
    {
        var unversioned = await _client!.GetAsync("/api/tools/timestamp-converter?iso=2021-01-01T00:00:00Z");
        unversioned.StatusCode.ShouldBe(HttpStatusCode.OK);
        await AssertJsonShape(unversioned);

        var versioned = await _client.GetAsync("/api/v1.0/tools/timestamp-converter?iso=2021-01-01T00:00:00Z");
        versioned.StatusCode.ShouldBe(HttpStatusCode.OK);
        await AssertJsonShape(versioned);
    }

    [Test]
    public async Task Should_Return400ProblemDetails_When_MissingBothParams()
    {
        var response = await _client!.GetAsync("/api/tools/timestamp-converter");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/problem+json");
    }

    [Test]
    public async Task Should_Return400ProblemDetails_When_BothParamsSupplied()
    {
        var response = await _client!.GetAsync(
            "/api/tools/timestamp-converter?epoch=1234&iso=2021-01-01T00:00:00Z");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/problem+json");
    }

    [Test]
    public async Task Should_Return400ProblemDetails_When_InvalidEpochFormat()
    {
        var response = await _client!.GetAsync("/api/tools/timestamp-converter?epoch=abc");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/problem+json");
    }

    [Test]
    public async Task Should_Return400ProblemDetails_When_InvalidIsoFormat()
    {
        var response = await _client!.GetAsync("/api/tools/timestamp-converter?iso=not-a-date");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/problem+json");
    }

    [Test]
    public async Task Should_AllowAnonymous_When_ApiKeyMiddlewareEnabled()
    {
        await using var factory = new ApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var unversioned = await client.GetAsync("/api/tools/timestamp-converter?epoch=1609459200");
        unversioned.StatusCode.ShouldBe(HttpStatusCode.OK);

        var versioned = await client.GetAsync("/api/v1.0/tools/timestamp-converter?epoch=1609459200");
        versioned.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    private static async Task AssertJsonShape(HttpResponseMessage response)
    {
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        doc.RootElement.TryGetProperty("epochSeconds", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("epochMilliseconds", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("iso8601", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("utcFormatted", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("localFormatted", out _).ShouldBeTrue();
        doc.RootElement.GetProperty("epochSeconds").GetInt64().ShouldBe(1609459200L);
    }
}
