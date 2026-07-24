using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UnitTests.Api;
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
        doc.RootElement.TryGetProperty("totalRequests", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("memoryUsageBytes", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("gcCollections", out _).ShouldBeTrue();
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
        doc.RootElement.TryGetProperty("totalRequests", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("memoryUsageBytes", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("gcCollections", out _).ShouldBeTrue();
    }

    [Test]
    public async Task Should_ExposeUptimeMetric_When_GetMetricsSummary()
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
    public async Task Should_ExposeTotalRequestsMetric_When_GetMetricsSummary()
    {
        var baselineResponse = await _client!.GetAsync("/api/metrics/summary");
        baselineResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var baseline = await baselineResponse.Content.ReadFromJsonAsync<MetricsSummaryResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        baseline.ShouldNotBeNull();

        await _client.GetAsync("/api/ping");
        await _client.GetAsync("/api/diagnostics");

        var response = await _client.GetAsync("/api/metrics/summary");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<MetricsSummaryResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        payload.ShouldNotBeNull();
        payload!.TotalRequests.ShouldBeGreaterThanOrEqualTo(0);
        payload.TotalRequests.ShouldBeGreaterThanOrEqualTo(baseline!.TotalRequests + 2);
    }

    [Test]
    public async Task Should_ExposeMemoryUsageMetric_When_GetMetricsSummary()
    {
        var response = await _client!.GetAsync("/api/metrics/summary");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<MetricsSummaryResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        payload.ShouldNotBeNull();
        payload!.MemoryUsageBytes.ShouldBeGreaterThan(0);
        payload.MemoryUsageBytes.ShouldBeLessThan(Environment.WorkingSet * 2);
    }

    [Test]
    public async Task Should_ExposeGcCollectionCounts_When_GetMetricsSummary()
    {
        var response = await _client!.GetAsync("/api/metrics/summary");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<MetricsSummaryResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        payload.ShouldNotBeNull();
        payload!.GcCollections.Gen0.ShouldBeGreaterThanOrEqualTo(0);
        payload.GcCollections.Gen1.ShouldBeGreaterThanOrEqualTo(0);
        payload.GcCollections.Gen2.ShouldBeGreaterThanOrEqualTo(0);
        payload.GcCollections.Gen0.ShouldBeGreaterThanOrEqualTo(payload.GcCollections.Gen1);
        payload.GcCollections.Gen1.ShouldBeGreaterThanOrEqualTo(payload.GcCollections.Gen2);
    }

    [Test]
    public async Task Should_RespectRateLimiting_When_MetricsSummaryEndpointCalled()
    {
        await using var unversionedFactory = new RateLimitedApiWebApplicationFactory();
        using var unversionedClient = unversionedFactory.CreateClient();

        var first = await unversionedClient.GetAsync("/api/metrics/summary");
        first.StatusCode.ShouldBe(HttpStatusCode.OK);

        var second = await unversionedClient.GetAsync("/api/metrics/summary");
        second.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);

        await using var versionedFactory = new RateLimitedApiWebApplicationFactory();
        using var versionedClient = versionedFactory.CreateClient();

        var versionedFirst = await versionedClient.GetAsync("/api/v1.0/metrics/summary");
        versionedFirst.StatusCode.ShouldBe(HttpStatusCode.OK);

        var versionedSecond = await versionedClient.GetAsync("/api/v1.0/metrics/summary");
        versionedSecond.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
    }

    [Test]
    public async Task Should_SupportConditionalGet_When_MetricsSummaryEndpointCalled()
    {
        await using var factory = new MetricsSummaryConditionalGetWebApplicationFactory();
        using var client = factory.CreateClient();

        var first = await client.GetAsync("/api/metrics/summary");
        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        var etag = first.Headers.ETag;
        etag.ShouldNotBeNull();

        using var second = new HttpRequestMessage(HttpMethod.Get, "/api/metrics/summary");
        second.Headers.IfNoneMatch.Add(etag!);
        var notModified = await client.SendAsync(second);
        notModified.StatusCode.ShouldBe(HttpStatusCode.NotModified);
        (await notModified.Content.ReadAsByteArrayAsync()).Length.ShouldBe(0);
    }
}
