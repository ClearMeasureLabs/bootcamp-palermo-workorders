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
    public async Task Should_Return200AndJson_When_GetUnversionedWithEpoch()
    {
        var response = await _client!.GetAsync("/api/tools/timestamp-converter?epoch=1711792800");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");
        var payload = await DeserializeAsync(response);
        payload.InputKind.ShouldBe("epoch");
        payload.EpochSeconds.ShouldBe(1711792800);
        payload.Iso8601Utc.ShouldNotBeNullOrWhiteSpace();
        payload.Utc.ShouldNotBeNullOrWhiteSpace();
        payload.Local.ShouldNotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Should_Return200AndJson_When_GetVersionedWithIso()
    {
        var response = await _client!.GetAsync("/api/v1.0/tools/timestamp-converter?iso=2026-07-12T15:00:00Z");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var payload = await DeserializeAsync(response);
        payload.InputKind.ShouldBe("iso");
        payload.EpochSeconds.ShouldBe(new DateTimeOffset(2026, 7, 12, 15, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds());
    }

    [Test]
    public async Task Should_Return400Problem_When_NoQueryParameters()
    {
        var response = await _client!.GetAsync("/api/tools/timestamp-converter");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.ShouldContain("detail");
    }

    [Test]
    public async Task Should_Return400Problem_When_BothEpochAndIsoProvided()
    {
        var response = await _client!.GetAsync(
            "/api/tools/timestamp-converter?epoch=1&iso=2026-01-01T00:00:00Z");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.ShouldContain("mutually exclusive");
    }

    [Test]
    public async Task Should_Return400Problem_When_EpochInvalid()
    {
        var response = await _client!.GetAsync("/api/tools/timestamp-converter?epoch=not-a-number");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.ShouldContain("detail");
    }

    [Test]
    public async Task Should_Return400Problem_When_IsoInvalid()
    {
        var response = await _client!.GetAsync("/api/tools/timestamp-converter?iso=garbage");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.ShouldContain("ISO-8601");
    }

    [Test]
    public async Task Should_Return200WithoutApiKey_When_MiddlewareEnabled()
    {
        await using var factory = new ApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var unversioned = await client.GetAsync("/api/tools/timestamp-converter?epoch=1711792800");
        unversioned.StatusCode.ShouldBe(HttpStatusCode.OK);

        var versioned = await client.GetAsync("/api/v1.0/tools/timestamp-converter?epoch=1711792800");
        versioned.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    private static async Task<TimestampConverterResponse> DeserializeAsync(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        var payload = JsonSerializer.Deserialize<TimestampConverterResponse>(
            json,
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        return payload!;
    }
}
