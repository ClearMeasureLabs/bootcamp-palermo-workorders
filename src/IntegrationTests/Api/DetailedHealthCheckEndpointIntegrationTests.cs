using System.Net;
using System.Text.Json;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

[TestFixture]
public class DetailedHealthCheckEndpointIntegrationTests
{
    private DetailedHealthWebApplicationFactory? _factory;
    private HttpClient? _client;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _factory = new DetailedHealthWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    [Test]
    public async Task Should_Return200AndJson_When_GetDetailedHealthCheckEndpoint()
    {
        var response = await _client!.GetAsync("/_healthcheck/detailed");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");
    }

    [Test]
    public async Task Should_IncludeOverallStatusAndEntries_When_DetailedHealthCheckReturns()
    {
        var response = await _client!.GetAsync("/_healthcheck/detailed");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);

        doc.RootElement.TryGetProperty("overallStatus", out var overallStatus).ShouldBeTrue();
        overallStatus.GetString().ShouldNotBeNullOrEmpty();

        doc.RootElement.TryGetProperty("totalDurationMs", out var totalDuration).ShouldBeTrue();
        totalDuration.GetDouble().ShouldBeGreaterThanOrEqualTo(0);

        doc.RootElement.TryGetProperty("entries", out var entries).ShouldBeTrue();
        entries.ValueKind.ShouldBe(JsonValueKind.Array);
        entries.GetArrayLength().ShouldBeGreaterThan(0);
    }

    [Test]
    public async Task Should_IncludeComponentDetails_When_DetailedHealthCheckReturns()
    {
        var response = await _client!.GetAsync("/_healthcheck/detailed");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        var entries = doc.RootElement.GetProperty("entries");

        foreach (var entry in entries.EnumerateArray())
        {
            entry.TryGetProperty("name", out _).ShouldBeTrue();
            entry.TryGetProperty("status", out var status).ShouldBeTrue();
            var statusString = status.GetString();
            (statusString == "Healthy" || statusString == "Degraded" || statusString == "Unhealthy").ShouldBeTrue();
            entry.TryGetProperty("durationMs", out _).ShouldBeTrue();
        }
    }

    [Test]
    public async Task Should_ListKnownComponentNames_When_DetailedHealthCheckReturns()
    {
        var response = await _client!.GetAsync("/_healthcheck/detailed");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        var entries = doc.RootElement.GetProperty("entries");
        var names = entries.EnumerateArray()
            .Select(e => e.GetProperty("name").GetString())
            .ToHashSet();

        names.ShouldContain("LlmGateway");
        names.ShouldContain("DataAccess");
        names.ShouldContain("Server");
        names.ShouldContain("API");
        names.ShouldContain("Jeffrey");
        names.ShouldContain("NeedsReboot");
    }
}
