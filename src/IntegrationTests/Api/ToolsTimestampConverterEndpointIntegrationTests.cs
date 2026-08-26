using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClearMeasure.Bootcamp.UnitTests.UI.Server;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

[TestFixture]
public class ToolsTimestampConverterEndpointIntegrationTests
{
    private const long KnownEpochSeconds = 1_700_000_000L;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

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
    public async Task Should_Return200AndJsonPayload_When_GetUnversioned_WithEpoch()
    {
        var response = await _client!.GetAsync($"/api/tools/timestamp-converter?epoch={KnownEpochSeconds}");

        await AssertValidPayloadAsync(response);
    }

    [Test]
    public async Task Should_Return200AndJsonPayload_When_GetVersioned_WithIso()
    {
        var iso = Uri.EscapeDataString(
            DateTimeOffset.FromUnixTimeSeconds(KnownEpochSeconds).UtcDateTime.ToString("O"));

        var response = await _client!.GetAsync($"/api/v1.0/tools/timestamp-converter?iso={iso}");

        await AssertValidPayloadAsync(response);
    }

    [TestCase("/api/tools/timestamp-converter")]
    [TestCase("/api/tools/timestamp-converter?epoch=abc")]
    [TestCase("/api/tools/timestamp-converter?iso=not-iso")]
    [TestCase("/api/tools/timestamp-converter?epoch=1700000000&iso=2023-01-01T00:00:00Z")]
    [TestCase("/api/v1.0/tools/timestamp-converter")]
    [TestCase("/api/v1.0/tools/timestamp-converter?epoch=not-a-number")]
    [TestCase("/api/v1.0/tools/timestamp-converter?iso=bad")]
    [TestCase("/api/v1.0/tools/timestamp-converter?epoch=1&iso=2023-01-01T00:00:00Z")]
    public async Task Should_Return400_When_MissingBothOrInvalid(string path)
    {
        var response = await _client!.GetAsync(path);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Should_Return200WithoutApiKey_When_MiddlewareEnabled()
    {
        await using var factory = new ApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var unversioned = await client.GetAsync($"/api/tools/timestamp-converter?epoch={KnownEpochSeconds}");
        await AssertValidPayloadAsync(unversioned);

        var iso = Uri.EscapeDataString(
            DateTimeOffset.FromUnixTimeSeconds(KnownEpochSeconds).UtcDateTime.ToString("O"));
        var versioned = await client.GetAsync($"/api/v1.0/tools/timestamp-converter?iso={iso}");
        await AssertValidPayloadAsync(versioned);
    }

    private static async Task AssertValidPayloadAsync(HttpResponseMessage response)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType.ShouldContain("application/json");

        var payload = await response.Content.ReadFromJsonAsync<TimestampConverterDto>(JsonOptions);
        payload.ShouldNotBeNull();
        payload.EpochSeconds.ShouldBe(KnownEpochSeconds);
        payload.EpochMilliseconds.ShouldBe(KnownEpochSeconds * 1000);
        payload.Iso8601.ShouldNotBeNullOrWhiteSpace();
        payload.Rfc1123.ShouldNotBeNullOrWhiteSpace();
        payload.UnixUtcDisplay.ShouldNotBeNullOrWhiteSpace();
        (payload.Iso8601.EndsWith("Z", StringComparison.Ordinal)
         || payload.Iso8601.EndsWith("+00:00", StringComparison.Ordinal)).ShouldBeTrue();
    }

    private sealed class TimestampConverterDto
    {
        public long EpochSeconds { get; set; }
        public long EpochMilliseconds { get; set; }
        public string? Iso8601 { get; set; }
        public string? Rfc1123 { get; set; }
        public string? UnixUtcDisplay { get; set; }
    }
}
