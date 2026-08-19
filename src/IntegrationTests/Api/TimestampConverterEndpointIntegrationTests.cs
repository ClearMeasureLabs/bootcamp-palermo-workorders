using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UnitTests.Api;
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
    public async Task Get_UnversionedRoute_Should_Return200WithJsonContent()
    {
        var response = await _client!.GetAsync("/api/tools/timestamp-converter?epoch=1609459200");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");

        var payload = await DeserializeResponse(response);
        payload.EpochSeconds.ShouldBe(1609459200);
        payload.Iso8601Utc.ShouldBe("2021-01-01T00:00:00.0000000+00:00");
        payload.Utc.ShouldBe("2021-01-01 00:00:00 UTC");
        payload.Local.ShouldNotBeNullOrWhiteSpace();
        payload.Relative.ShouldNotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Get_VersionedRoute_Should_Return200WithJsonContent()
    {
        var response = await _client!.GetAsync("/api/v1.0/tools/timestamp-converter?iso=2021-01-01T00:00:00Z");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");

        var payload = await DeserializeResponse(response);
        payload.EpochSeconds.ShouldBe(1609459200);
        payload.Iso8601Utc.ShouldBe("2021-01-01T00:00:00.0000000+00:00");
    }

    [Test]
    public async Task Get_AnonymousAccess_Should_Succeed()
    {
        await using var factory = new ApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var unversioned = await client.GetAsync("/api/tools/timestamp-converter?epoch=1609459200");
        unversioned.StatusCode.ShouldBe(HttpStatusCode.OK);

        var versioned = await client.GetAsync("/api/v1.0/tools/timestamp-converter?epoch=1609459200");
        versioned.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task Get_InvalidEpoch_Should_Return400()
    {
        var response = await _client!.GetAsync("/api/tools/timestamp-converter?epoch=not-a-number");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("error").GetString().ShouldBe("Invalid epoch value.");
    }

    [Test]
    public async Task Get_RateLimitingApplies()
    {
        var settings = new Dictionary<string, string?>
        {
            ["ApiRateLimiting:Enabled"] = "true",
            ["ApiRateLimiting:PermitLimit"] = "1",
            ["ApiRateLimiting:WindowSeconds"] = "60",
            ["ApiRateLimiting:SegmentsPerWindow"] = "2",
            ["ApiRateLimiting:QueueLimit"] = "0",
            ["ApiRateLimiting:ApiKeyHeaderName"] = "X-API-Key"
        };

        await using var factory = new TunableApiRateLimitWebApplicationFactory(settings);
        using var client = factory.CreateClient();

        (await client.GetAsync("/api/tools/timestamp-converter?epoch=1609459200")).StatusCode
            .ShouldBe(HttpStatusCode.OK);

        (await client.GetAsync("/api/tools/timestamp-converter?epoch=1609459200")).StatusCode
            .ShouldBe(HttpStatusCode.TooManyRequests);
    }

    private static async Task<TimestampConverterResponse> DeserializeResponse(HttpResponseMessage response)
    {
        var payload = await response.Content.ReadFromJsonAsync<TimestampConverterResponse>(
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        return payload!;
    }
}
