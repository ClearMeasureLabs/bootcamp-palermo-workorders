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
    public async Task Should_Return200AndJson_When_GetMetricsSummaryUnversioned()
    {
        var response = await _client!.GetAsync("/api/metrics/summary");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        doc.RootElement.TryGetProperty("uptime", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("totalRequestsServed", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("managedMemoryBytes", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("workingSetBytes", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("gcCollectionCounts", out var gc).ShouldBeTrue();
        gc.TryGetProperty("gen0", out _).ShouldBeTrue();
        gc.TryGetProperty("gen1", out _).ShouldBeTrue();
        gc.TryGetProperty("gen2", out _).ShouldBeTrue();
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
        doc.RootElement.TryGetProperty("uptime", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("totalRequestsServed", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("managedMemoryBytes", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("workingSetBytes", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("gcCollectionCounts", out _).ShouldBeTrue();
    }

    [Test]
    public async Task Should_ExposeNonNegativeUptime_When_GetMetricsSummary()
    {
        var before = DateTime.UtcNow;
        var response = await _client!.GetAsync("/api/metrics/summary");
        var after = DateTime.UtcNow;
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<MetricsSummaryResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        payload.ShouldNotBeNull();
        payload!.Uptime.ShouldBeGreaterThanOrEqualTo(TimeSpan.Zero);
        var health = SimpleHealthResponseBuilder.Build(TimeProvider.System);
        (after - before).ShouldBeLessThan(TimeSpan.FromSeconds(30));
        payload.Uptime.ShouldBeLessThanOrEqualTo(health.Uptime + TimeSpan.FromSeconds(2));
        payload.Uptime.ShouldBeGreaterThanOrEqualTo(health.Uptime - TimeSpan.FromSeconds(2));
    }

    [Test]
    public async Task Should_ExposeNonNegativeMemoryAndGcCounts_When_GetMetricsSummary()
    {
        var response = await _client!.GetAsync("/api/metrics/summary");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<MetricsSummaryResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        payload.ShouldNotBeNull();
        payload!.ManagedMemoryBytes.ShouldBeGreaterThanOrEqualTo(0);
        payload.WorkingSetBytes.ShouldBeGreaterThanOrEqualTo(0);
        payload.GcCollectionCounts.Gen0.ShouldBeGreaterThanOrEqualTo(0);
        payload.GcCollectionCounts.Gen1.ShouldBeGreaterThanOrEqualTo(0);
        payload.GcCollectionCounts.Gen2.ShouldBeGreaterThanOrEqualTo(0);
    }

    [Test]
    public async Task Should_IncrementTotalRequestsServed_When_TrafficOccurs()
    {
        var baselineResponse = await _client!.GetAsync("/api/metrics/summary");
        baselineResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var baseline = await baselineResponse.Content.ReadFromJsonAsync<MetricsSummaryResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        baseline.ShouldNotBeNull();

        const int additionalRequests = 3;
        for (var i = 0; i < additionalRequests; i++)
        {
            var ping = await _client.GetAsync("/api/ping");
            ping.StatusCode.ShouldBe(HttpStatusCode.OK);
        }

        var afterResponse = await _client.GetAsync("/api/metrics/summary");
        afterResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var after = await afterResponse.Content.ReadFromJsonAsync<MetricsSummaryResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        after.ShouldNotBeNull();
        after!.TotalRequestsServed.ShouldBeGreaterThanOrEqualTo(baseline!.TotalRequestsServed + additionalRequests);
    }

    [Test]
    public async Task Should_EnforceApiKey_When_MiddlewareEnabledAndMetricsProtected()
    {
        await using var factory = new DiagnosticsApiKeyProtectedWebApplicationFactory();
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

    [Test]
    public async Task Should_NotConflictWithExistingDiagnosticsRoutes_When_MetricsRegistered()
    {
        var diagnostics = await _client!.GetAsync("/api/diagnostics");
        diagnostics.StatusCode.ShouldBe(HttpStatusCode.OK);

        var metrics = await _client.GetAsync("/api/metrics/summary");
        metrics.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
