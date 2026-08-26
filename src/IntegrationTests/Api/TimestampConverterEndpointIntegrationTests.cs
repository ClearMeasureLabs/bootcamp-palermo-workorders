using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClearMeasure.Bootcamp.UnitTests.UI.Server;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

[TestFixture]
public class TimestampConverterEndpointIntegrationTests
{
    private const long KnownUnixSeconds = 1_700_000_000L;
    private const string KnownIso = "2023-11-14T22:13:20Z";
    private const string KnownHuman = "Tuesday, 14 November 2023 22:13:20 UTC";

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
    public async Task Should_Return200AndGoldenJson_When_GetUnversioned_WithUnix()
    {
        var response = await _client!.GetAsync($"/api/tools/timestamp-converter?unix={KnownUnixSeconds}");

        await AssertGoldenAsync(response);
    }

    [Test]
    public async Task Should_Return200AndGoldenJson_When_GetVersioned_WithIso()
    {
        var response = await _client!.GetAsync(
            $"/api/v1.0/tools/timestamp-converter?iso={Uri.EscapeDataString(KnownIso)}");

        await AssertGoldenAsync(response);
    }

    [TestCase("/api/tools/timestamp-converter")]
    [TestCase("/api/tools/timestamp-converter?unix=1700000000&iso=2023-11-14T22:13:20Z")]
    [TestCase("/api/v1.0/tools/timestamp-converter")]
    [TestCase("/api/v1.0/tools/timestamp-converter?unix=1&iso=2023-01-01T00:00:00Z")]
    public async Task Should_Return400_When_NeitherOrBoth(string path)
    {
        var response = await _client!.GetAsync(path);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Should_Return200WithoutApiKey_When_MiddlewareEnabled()
    {
        await using var factory = new ApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var unversioned = await client.GetAsync($"/api/tools/timestamp-converter?unix={KnownUnixSeconds}");
        await AssertGoldenAsync(unversioned);

        var versioned = await client.GetAsync(
            $"/api/v1.0/tools/timestamp-converter?iso={Uri.EscapeDataString(KnownIso)}");
        await AssertGoldenAsync(versioned);
    }

    private static async Task AssertGoldenAsync(HttpResponseMessage response)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType.ShouldContain("application/json");

        var payload = await response.Content.ReadFromJsonAsync<TimestampConverterDto>(JsonOptions);
        payload.ShouldNotBeNull();
        payload.Unix.ShouldBe(KnownUnixSeconds);
        payload.Iso.ShouldBe(KnownIso);
        payload.Human.ShouldBe(KnownHuman);
    }

    private sealed class TimestampConverterDto
    {
        public long Unix { get; set; }
        public string? Iso { get; set; }
        public string? Human { get; set; }
    }
}
