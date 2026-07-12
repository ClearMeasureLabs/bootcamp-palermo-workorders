using System.Net;
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
    public async Task Should_Return200AndJson_When_EpochQueryUnversioned()
    {
        var response = await _client!.GetAsync("/api/tools/timestamp-converter?epoch=1718208000");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        doc.RootElement.TryGetProperty("inputKind", out var inputKind).ShouldBeTrue();
        inputKind.GetString().ShouldBe("epoch");
        doc.RootElement.TryGetProperty("epochSeconds", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("epochMilliseconds", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("iso8601", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("utcDisplay", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("localDisplay", out _).ShouldBeTrue();
    }

    [Test]
    public async Task Should_Return200AndJson_When_IsoQueryVersioned()
    {
        var response = await _client!.GetAsync("/api/v1.0/tools/timestamp-converter?iso=2024-06-12T12:00:00Z");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        doc.RootElement.TryGetProperty("inputKind", out var inputKind).ShouldBeTrue();
        inputKind.GetString().ShouldBe("iso");
        doc.RootElement.GetProperty("epochSeconds").GetInt64().ShouldBe(1718208000L);
    }

    [Test]
    public async Task Should_Return200AndConsistentValues_When_EpochMillisecondsQuery()
    {
        var secondsResponse = await _client!.GetAsync("/api/tools/timestamp-converter?epoch=1718208000");
        var millisResponse = await _client.GetAsync("/api/tools/timestamp-converter?epoch=1718208000000");

        secondsResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        millisResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        await using var secondsStream = await secondsResponse.Content.ReadAsStreamAsync();
        await using var millisStream = await millisResponse.Content.ReadAsStreamAsync();
        using var secondsDoc = await JsonDocument.ParseAsync(secondsStream);
        using var millisDoc = await JsonDocument.ParseAsync(millisStream);

        secondsDoc.RootElement.GetProperty("epochSeconds").GetInt64()
            .ShouldBe(millisDoc.RootElement.GetProperty("epochSeconds").GetInt64());
        secondsDoc.RootElement.GetProperty("iso8601").GetString()
            .ShouldBe(millisDoc.RootElement.GetProperty("iso8601").GetString());
    }

    [Test]
    public async Task Should_Return400_When_NoQueryParameters()
    {
        var response = await _client!.GetAsync("/api/tools/timestamp-converter");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/problem+json");

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        doc.RootElement.TryGetProperty("detail", out _).ShouldBeTrue();
    }

    [Test]
    public async Task Should_Return400_When_BothParametersProvided()
    {
        var response = await _client!.GetAsync(
            "/api/tools/timestamp-converter?epoch=1&iso=2024-01-01T00:00:00Z");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/problem+json");

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        doc.RootElement.TryGetProperty("detail", out var detail).ShouldBeTrue();
        detail.GetString().ShouldNotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Should_Return400_When_InvalidEpoch()
    {
        var response = await _client!.GetAsync("/api/tools/timestamp-converter?epoch=notnumeric");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/problem+json");
    }

    [Test]
    public async Task Should_Return400_When_InvalidIso()
    {
        var response = await _client!.GetAsync("/api/tools/timestamp-converter?iso=not-a-date");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/problem+json");
    }

    [Test]
    public async Task Should_Return200WithoutApiKey_When_MiddlewareEnabled()
    {
        await using var factory = new ApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var unversioned = await client.GetAsync("/api/tools/timestamp-converter?epoch=1718208000");
        unversioned.StatusCode.ShouldBe(HttpStatusCode.OK);

        var versioned = await client.GetAsync("/api/v1.0/tools/timestamp-converter?epoch=1718208000");
        versioned.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
