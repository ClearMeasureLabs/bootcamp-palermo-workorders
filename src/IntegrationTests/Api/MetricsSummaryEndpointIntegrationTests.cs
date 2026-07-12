using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Server.RateLimiting;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UnitTests.Api;
using ClearMeasure.Bootcamp.UnitTests.UI.Server;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
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
        AssertMetricsSummaryShape(doc.RootElement);
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
        AssertMetricsSummaryShape(doc.RootElement);
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
        payload!.CurrentMemoryBytes.ShouldBeGreaterThanOrEqualTo(0);
        payload.GcCollections.Gen0.ShouldBeGreaterThanOrEqualTo(0);
        payload.GcCollections.Gen1.ShouldBeGreaterThanOrEqualTo(0);
        payload.GcCollections.Gen2.ShouldBeGreaterThanOrEqualTo(0);
    }

    [Test]
    public async Task Should_ReflectPriorHttpRequests_When_GetMetricsSummaryAfterCalls()
    {
        await using var factory = new DiagnosticsWebApplicationFactory();
        using var client = factory.CreateClient();

        const int priorCalls = 3;
        for (var i = 0; i < priorCalls; i++)
            (await client.GetAsync("/api/ping")).StatusCode.ShouldBe(HttpStatusCode.OK);

        var response = await client.GetAsync("/api/metrics/summary");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<MetricsSummaryResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        payload.ShouldNotBeNull();
        payload!.TotalRequestsServed.ShouldBeGreaterThanOrEqualTo(priorCalls + 1);
    }

    [Test]
    public async Task Should_CountFailedRequests_When_PipelineReturnsError()
    {
        await using var factory = new DiagnosticsWebApplicationFactory();
        using var client = factory.CreateClient();

        var baseline = await client.GetAsync("/api/metrics/summary");
        baseline.StatusCode.ShouldBe(HttpStatusCode.OK);
        var baselinePayload = await baseline.Content.ReadFromJsonAsync<MetricsSummaryResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        baselinePayload.ShouldNotBeNull();

        (await client.GetAsync("/api/no-such-route-for-metrics-test")).StatusCode.ShouldBe(HttpStatusCode.NotFound);

        var after = await client.GetAsync("/api/metrics/summary");
        after.StatusCode.ShouldBe(HttpStatusCode.OK);
        var afterPayload = await after.Content.ReadFromJsonAsync<MetricsSummaryResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        afterPayload.ShouldNotBeNull();
        afterPayload!.TotalRequestsServed.ShouldBeGreaterThanOrEqualTo(baselinePayload!.TotalRequestsServed + 2);
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
    public async Task Should_ApplyApiRateLimiting_When_PathIsUnderApi()
    {
        var settings = new Dictionary<string, string?>
        {
            ["ApiRateLimiting:Enabled"] = "true",
            ["ApiRateLimiting:PermitLimit"] = "2",
            ["ApiRateLimiting:WindowSeconds"] = "2",
            ["ApiRateLimiting:SegmentsPerWindow"] = "2",
            ["ApiRateLimiting:QueueLimit"] = "0",
            ["ApiRateLimiting:ApiKeyHeaderName"] = "X-API-Key"
        };
        await using var factory = new TunableApiRateLimitWebApplicationFactory(settings);
        using var client = factory.CreateClient();

        (await client.GetAsync("/api/metrics/summary")).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await client.GetAsync("/api/metrics/summary")).StatusCode.ShouldBe(HttpStatusCode.OK);

        var limited = await client.GetAsync("/api/metrics/summary");
        limited.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        limited.Headers.TryGetValues("Retry-After", out _).ShouldBeTrue();
        limited.Headers.TryGetValues(RateLimitingMiddleware.HeaderLimit, out _).ShouldBeTrue();
    }

    [Test]
    public async Task Should_NotConflictWithDiagnostics_When_BothRoutesRegistered()
    {
        var diagnostics = await _client!.GetAsync("/api/diagnostics");
        diagnostics.StatusCode.ShouldBe(HttpStatusCode.OK);

        var diagnosticsPayload = await diagnostics.Content.ReadFromJsonAsync<DiagnosticsResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        diagnosticsPayload.ShouldNotBeNull();
        diagnosticsPayload!.Environment.ShouldBe("Testing");

        var metrics = await _client.GetAsync("/api/metrics/summary");
        metrics.StatusCode.ShouldBe(HttpStatusCode.OK);

        await using var metricsStream = await metrics.Content.ReadAsStreamAsync();
        using var metricsDoc = await JsonDocument.ParseAsync(metricsStream);
        metricsDoc.RootElement.TryGetProperty("environment", out _).ShouldBeFalse();
        metricsDoc.RootElement.TryGetProperty("featureFlags", out _).ShouldBeFalse();
        AssertMetricsSummaryShape(metricsDoc.RootElement);
    }

    private static void AssertMetricsSummaryShape(JsonElement root)
    {
        root.TryGetProperty("uptime", out _).ShouldBeTrue();
        root.TryGetProperty("totalRequestsServed", out _).ShouldBeTrue();
        root.TryGetProperty("currentMemoryBytes", out _).ShouldBeTrue();
        root.TryGetProperty("gcCollections", out var gc).ShouldBeTrue();
        gc.TryGetProperty("gen0", out _).ShouldBeTrue();
        gc.TryGetProperty("gen1", out _).ShouldBeTrue();
        gc.TryGetProperty("gen2", out _).ShouldBeTrue();
    }
}
