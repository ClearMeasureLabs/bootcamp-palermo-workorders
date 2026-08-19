using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UnitTests.Api;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

[TestFixture]
public class GuidGeneratorEndpointIntegrationTests
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
    public async Task Should_Return200AndJsonGuids_When_PostUnversioned()
    {
        var response = await _client!.PostAsJsonAsync("/api/tools/guid-generator", new { });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        doc.RootElement.TryGetProperty("guids", out var guids).ShouldBeTrue();
        guids.GetArrayLength().ShouldBe(1);
        Guid.TryParseExact(guids[0].GetString(), "D", out _).ShouldBeTrue();
    }

    [Test]
    public async Task Should_Return200AndJsonGuids_When_PostVersioned()
    {
        var response = await _client!.PostAsJsonAsync("/api/v1.0/tools/guid-generator", new { });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        doc.RootElement.TryGetProperty("guids", out var guids).ShouldBeTrue();
        guids.GetArrayLength().ShouldBe(1);
    }

    [Test]
    public async Task Should_ReturnMultipleGuids_When_PostWithCount()
    {
        var response = await _client!.PostAsJsonAsync("/api/tools/guid-generator", new { count = 3 });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<GuidGeneratorResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        payload.ShouldNotBeNull();
        payload!.Guids.Length.ShouldBe(3);
        foreach (var guid in payload.Guids)
            Guid.TryParseExact(guid, "D", out _).ShouldBeTrue();
    }

    [Test]
    public async Task Should_ReturnBadRequest_When_CountOutOfRange()
    {
        var response = await _client!.PostAsJsonAsync("/api/tools/guid-generator", new { count = 101 });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        doc.RootElement.TryGetProperty("status", out var status).ShouldBeTrue();
        status.GetInt32().ShouldBe(400);
    }

    [Test]
    public async Task Should_AllowAnonymousAccess_When_PostGuidGenerator()
    {
        using var anonymousClient = _factory!.CreateClient();
        var response = await anonymousClient.PostAsJsonAsync("/api/tools/guid-generator", new { count = 1 });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task Should_EnforceRateLimit_When_PostRepeatedly()
    {
        var settings = new Dictionary<string, string?>
        {
            ["ApiRateLimiting:Enabled"] = "true",
            ["ApiRateLimiting:PermitLimit"] = "2",
            ["ApiRateLimiting:WindowSeconds"] = "60",
            ["ApiRateLimiting:SegmentsPerWindow"] = "2",
            ["ApiRateLimiting:QueueLimit"] = "0"
        };
        await using var factory = new TunableApiRateLimitWebApplicationFactory(settings);
        using var client = factory.CreateClient();

        (await client.PostAsJsonAsync("/api/tools/guid-generator", new { count = 1 })).StatusCode
            .ShouldBe(HttpStatusCode.OK);
        (await client.PostAsJsonAsync("/api/tools/guid-generator", new { count = 1 })).StatusCode
            .ShouldBe(HttpStatusCode.OK);
        (await client.PostAsJsonAsync("/api/tools/guid-generator", new { count = 1 })).StatusCode
            .ShouldBe(HttpStatusCode.TooManyRequests);
    }
}
