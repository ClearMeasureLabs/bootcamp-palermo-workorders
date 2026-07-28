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
    public async Task Should_Return200AndJson_When_GetUnversioned()
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
        doc.RootElement.TryGetProperty("gcMemoryMb", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("workingSetMb", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("gcCollections", out _).ShouldBeTrue();
    }

    [Test]
    public async Task Should_Return200AndJson_When_GetVersioned()
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
        doc.RootElement.TryGetProperty("gcMemoryMb", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("workingSetMb", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("gcCollections", out _).ShouldBeTrue();
    }

    [Test]
    public async Task Should_ExposeUptimeNonNegative_When_GetMetrics()
    {
        var first = await _client!.GetAsync("/api/metrics/summary");
        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        var firstPayload = await first.Content.ReadFromJsonAsync<MetricsSummaryResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        firstPayload.ShouldNotBeNull();
        firstPayload!.Uptime.ShouldBeGreaterThanOrEqualTo(TimeSpan.Zero);

        await Task.Delay(50);

        var second = await _client.GetAsync("/api/metrics/summary");
        second.StatusCode.ShouldBe(HttpStatusCode.OK);
        var secondPayload = await second.Content.ReadFromJsonAsync<MetricsSummaryResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        secondPayload.ShouldNotBeNull();
        secondPayload!.Uptime.ShouldBeGreaterThanOrEqualTo(firstPayload.Uptime);
    }

    [Test]
    public async Task Should_ExposeTotalRequests_IncreasingAfterProbes_When_GetMetrics()
    {
        var baseline = await _client!.GetAsync("/api/metrics/summary");
        baseline.StatusCode.ShouldBe(HttpStatusCode.OK);
        var baselinePayload = await baseline.Content.ReadFromJsonAsync<MetricsSummaryResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        baselinePayload.ShouldNotBeNull();
        var startCount = baselinePayload!.TotalRequests;

        for (var i = 0; i < 3; i++)
        {
            var probe = await _client.GetAsync("/api/metrics/summary");
            probe.StatusCode.ShouldBe(HttpStatusCode.OK);
        }

        var after = await _client.GetAsync("/api/metrics/summary");
        after.StatusCode.ShouldBe(HttpStatusCode.OK);
        var afterPayload = await after.Content.ReadFromJsonAsync<MetricsSummaryResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        afterPayload.ShouldNotBeNull();
        afterPayload!.TotalRequests.ShouldBeGreaterThanOrEqualTo(startCount + 3);
    }

    [Test]
    public async Task Should_ExposeGcCountersNonNegative_When_GetMetrics()
    {
        var response = await _client!.GetAsync("/api/metrics/summary");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<MetricsSummaryResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        payload.ShouldNotBeNull();
        payload!.GcCollections.Gen0.ShouldBeGreaterThanOrEqualTo(0);
        payload.GcCollections.Gen1.ShouldBeGreaterThanOrEqualTo(0);
        payload.GcCollections.Gen2.ShouldBeGreaterThanOrEqualTo(0);
    }

    [Test]
    public async Task Should_ExposeMemoryFieldsNonNegative_When_GetMetrics()
    {
        var response = await _client!.GetAsync("/api/metrics/summary");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<MetricsSummaryResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        payload.ShouldNotBeNull();
        payload!.GcMemoryMb.ShouldBeGreaterThan(0);
        payload.WorkingSetMb.ShouldBeGreaterThan(0);
    }

    [Test]
    public async Task Should_Return401_When_ApiKeyRequired()
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
