using System.Globalization;
using System.Net;
using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using ClearMeasure.Bootcamp.UnitTests.UI.Server;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

[TestFixture]
public class TimestampConverterEndpointIntegrationTests
{
    private const long KnownEpochSeconds = 1711800000L;

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
    public async Task Should_Return200Json_When_GetUnversionedWithEpoch()
    {
        var response = await _client!.GetAsync(
            $"/api/tools/timestamp-converter?value={KnownEpochSeconds.ToString(CultureInfo.InvariantCulture)}");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");
        var payload = await DeserializeAsync(response);
        payload.UnixEpochSeconds.ShouldBe(KnownEpochSeconds);
        payload.Iso8601Utc.ShouldNotBeNullOrWhiteSpace();
        payload.UtcDisplay.ShouldNotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Should_Return200Json_When_GetVersionedWithIso()
    {
        var iso = DateTimeOffset.FromUnixTimeSeconds(KnownEpochSeconds)
            .ToString("O", CultureInfo.InvariantCulture);
        var response = await _client!.GetAsync(
            $"/api/v1.0/tools/timestamp-converter?value={Uri.EscapeDataString(iso)}");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var payload = await DeserializeAsync(response);
        payload.UnixEpochSeconds.ShouldBe(KnownEpochSeconds);
        payload.Iso8601Utc.ShouldBe(iso);
    }

    [Test]
    public async Task Should_Return200WithoutApiKey_When_MiddlewareEnabled()
    {
        await using var factory = new ApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();
        var query = KnownEpochSeconds.ToString(CultureInfo.InvariantCulture);

        var unversioned = await client.GetAsync($"/api/tools/timestamp-converter?value={query}");
        unversioned.StatusCode.ShouldBe(HttpStatusCode.OK);

        var versioned = await client.GetAsync($"/api/v1.0/tools/timestamp-converter?value={query}");
        versioned.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task Should_Return400_When_ValueInvalid()
    {
        var response = await _client!.GetAsync("/api/tools/timestamp-converter?value=not-a-timestamp");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.ShouldContain("detail");
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
