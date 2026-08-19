using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UnitTests.UI.Server;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

[TestFixture]
public class MetricsEndpointIntegrationTests
{
    private MetricsWebApplicationFactory? _factory;
    private HttpClient? _client;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _factory = new MetricsWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    [Test]
    public async Task Should_Return200AndJson_When_GetMetricsUnversioned()
    {
        var response = await _client!.GetAsync("/api/metrics/summary");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        AssertAllTopLevelProperties(doc.RootElement);
    }

    [Test]
    public async Task Should_Return200AndJson_When_GetMetricsVersioned()
    {
        var response = await _client!.GetAsync("/api/v1.0/metrics/summary");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        AssertAllTopLevelProperties(doc.RootElement);
    }

    [Test]
    public async Task Should_ExposeNonNegativeUptime_When_GetMetrics()
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
    public async Task Should_ExposeTotalRequestsAsNonNegativeInteger_When_GetMetrics()
    {
        var response = await _client!.GetAsync("/api/metrics/summary");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<MetricsSummaryResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        payload.ShouldNotBeNull();
        payload!.TotalRequests.ShouldBeGreaterThanOrEqualTo(0);
    }

    [Test]
    public async Task Should_ExposeMemoryAndGcMetrics_When_GetMetrics()
    {
        var response = await _client!.GetAsync("/api/metrics/summary");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<MetricsSummaryResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        payload.ShouldNotBeNull();
        payload!.GcMemoryMb.ShouldBeGreaterThan(0);
        payload.WorkingSetMb.ShouldBeGreaterThan(0);
        payload.GcCollectionCounts.Gen0.ShouldBeGreaterThanOrEqualTo(0);
        payload.GcCollectionCounts.Gen1.ShouldBeGreaterThanOrEqualTo(0);
        payload.GcCollectionCounts.Gen2.ShouldBeGreaterThanOrEqualTo(0);
    }

    [Test]
    public async Task Should_IncrementTotalRequests_OnEachRequest_When_MultipleGets()
    {
        var firstResponse = await _client!.GetAsync("/api/metrics/summary");
        firstResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var first = await firstResponse.Content.ReadFromJsonAsync<MetricsSummaryResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        first.ShouldNotBeNull();

        var secondResponse = await _client.GetAsync("/api/metrics/summary");
        secondResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var second = await secondResponse.Content.ReadFromJsonAsync<MetricsSummaryResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        second.ShouldNotBeNull();

        second!.TotalRequests.ShouldBeGreaterThan(first!.TotalRequests);
    }

    [Test]
    public async Task Should_Require401Unauthorized_When_NoApiKeyAndAuthEnabled()
    {
        await using var factory = new MetricsApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var unauth = await client.GetAsync("/api/metrics/summary");
        unauth.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        var unauthVersioned = await client.GetAsync("/api/v1.0/metrics/summary");
        unauthVersioned.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Should_Return200_When_ValidApiKeyProvided_AndAuthEnabled()
    {
        await using var factory = new MetricsApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(
            ApiKeyConstants.HeaderName,
            ApiKeyProtectedWebApplicationFactory.TestApiKey);

        var ok = await client.GetAsync("/api/metrics/summary");
        ok.StatusCode.ShouldBe(HttpStatusCode.OK);

        var okVersioned = await client.GetAsync("/api/v1.0/metrics/summary");
        okVersioned.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    private static void AssertAllTopLevelProperties(JsonElement root)
    {
        root.TryGetProperty("uptime", out _).ShouldBeTrue();
        root.TryGetProperty("totalRequests", out _).ShouldBeTrue();
        root.TryGetProperty("gcMemoryMb", out _).ShouldBeTrue();
        root.TryGetProperty("workingSetMb", out _).ShouldBeTrue();
        root.TryGetProperty("gcCollectionCounts", out var gcCounts).ShouldBeTrue();
        gcCounts.TryGetProperty("gen0", out _).ShouldBeTrue();
        gcCounts.TryGetProperty("gen1", out _).ShouldBeTrue();
        gcCounts.TryGetProperty("gen2", out _).ShouldBeTrue();
    }
}
