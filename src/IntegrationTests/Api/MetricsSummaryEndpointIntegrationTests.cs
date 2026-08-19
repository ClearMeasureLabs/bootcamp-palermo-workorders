using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UnitTests.UI.Server;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

[TestFixture]
public class MetricsSummaryEndpointIntegrationTests
{
    private MetricsSummaryWebApplicationFactory? _factory;
    private HttpClient? _client;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _factory = new MetricsSummaryWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    [Test]
    public async Task Should_Return200AndJson_When_GetMetricsSummaryUnversioned()
    {
        var response = await _client!.GetAsync("/api/metrics/summary");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        doc.RootElement.TryGetProperty("environment", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("uptime", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("totalRequestsServed", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("memoryMb", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("gcCollectionCounts", out _).ShouldBeTrue();
    }

    [Test]
    public async Task Should_Return200AndJson_When_GetMetricsSummaryVersioned()
    {
        var response = await _client!.GetAsync("/api/v1.0/metrics/summary");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        doc.RootElement.TryGetProperty("environment", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("uptime", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("totalRequestsServed", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("memoryMb", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("gcCollectionCounts", out _).ShouldBeTrue();
    }

    [Test]
    public async Task Should_ExposeNonNegativeMetrics_When_GetMetricsSummary()
    {
        var before = DateTime.UtcNow;
        var response = await _client!.GetAsync("/api/metrics/summary");
        var after = DateTime.UtcNow;
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<MetricsSummaryResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        payload.ShouldNotBeNull();
        payload!.TotalRequestsServed.ShouldBeGreaterThanOrEqualTo(0);
        payload.MemoryMb.ShouldBeGreaterThanOrEqualTo(0);
        payload.GcCollectionCounts.Gen0Count.ShouldBeGreaterThanOrEqualTo(0);
        payload.GcCollectionCounts.Gen1Count.ShouldBeGreaterThanOrEqualTo(0);
        payload.GcCollectionCounts.Gen2Count.ShouldBeGreaterThanOrEqualTo(0);
        payload.Uptime.ShouldBeGreaterThanOrEqualTo(TimeSpan.Zero);
        var health = SimpleHealthResponseBuilder.Build(TimeProvider.System);
        (after - before).ShouldBeLessThan(TimeSpan.FromSeconds(30));
        payload.Uptime.ShouldBeLessThanOrEqualTo(health.Uptime + TimeSpan.FromSeconds(2));
        payload.Uptime.ShouldBeGreaterThanOrEqualTo(health.Uptime - TimeSpan.FromSeconds(2));
    }

    [Test]
    public async Task Should_IncrementRequestCount_Across_Requests_When_Subsequent_CallsMade()
    {
        var firstResponse = await _client!.GetAsync("/api/metrics/summary");
        firstResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var firstPayload = await firstResponse.Content.ReadFromJsonAsync<MetricsSummaryResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        firstPayload.ShouldNotBeNull();

        var secondResponse = await _client.GetAsync("/api/metrics/summary");
        secondResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var secondPayload = await secondResponse.Content.ReadFromJsonAsync<MetricsSummaryResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        secondPayload.ShouldNotBeNull();

        secondPayload!.TotalRequestsServed.ShouldBeGreaterThan(firstPayload!.TotalRequestsServed);
    }

    [Test]
    public async Task Should_EnforceApiKey_When_ApiKeyAuthenticationEnabled()
    {
        await using var factory = new MetricsSummaryApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var unauth = await client.GetAsync("/api/metrics/summary");
        unauth.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        var unauthVersioned = await client.GetAsync("/api/v1.0/metrics/summary");
        unauthVersioned.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        using var withKey = factory.CreateClient();
        withKey.DefaultRequestHeaders.Add(
            ApiKeyConstants.HeaderName,
            ApiKeyProtectedWebApplicationFactory.TestApiKey);

        var ok = await withKey.GetAsync("/api/metrics/summary");
        ok.StatusCode.ShouldBe(HttpStatusCode.OK);

        var okVersioned = await withKey.GetAsync("/api/v1.0/metrics/summary");
        okVersioned.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
