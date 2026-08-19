using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UnitTests.UI.Server;
using Microsoft.AspNetCore.Mvc.Testing;
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
        AssertRequiredProperties(doc.RootElement);
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
        AssertRequiredProperties(doc.RootElement);
    }

    [Test]
    public async Task Should_ExposeNonNegativeValues_When_GetMetricsSummary()
    {
        var response = await _client!.GetAsync("/api/metrics/summary");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<MetricsSummaryResponse>(
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload!.Uptime.ShouldBeGreaterThanOrEqualTo(TimeSpan.Zero);
        payload.TotalRequestsServed.ShouldBeGreaterThan(0);
        payload.WorkingSetMb.ShouldBeGreaterThan(0);
        payload.GcMemoryMb.ShouldBeGreaterThanOrEqualTo(0);
        payload.GcCollectionCounts.Gen0.ShouldBeGreaterThanOrEqualTo(0);
        payload.GcCollectionCounts.Gen1.ShouldBeGreaterThanOrEqualTo(0);
        payload.GcCollectionCounts.Gen2.ShouldBeGreaterThanOrEqualTo(0);
        payload.CapturedAtUtc.ShouldBeGreaterThan(DateTime.MinValue);
    }

    [Test]
    public async Task Should_IncrementTotalRequestsServed_When_SequentialGets()
    {
        await using var factory = new MetricsSummaryWebApplicationFactory();
        using var client = factory.CreateClient();

        var firstResponse = await client.GetAsync("/api/metrics/summary");
        firstResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var first = await firstResponse.Content.ReadFromJsonAsync<MetricsSummaryResponse>(
            ConditionalGetEtag.JsonSerializerOptions);
        first.ShouldNotBeNull();

        var secondResponse = await client.GetAsync("/api/metrics/summary");
        secondResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var second = await secondResponse.Content.ReadFromJsonAsync<MetricsSummaryResponse>(
            ConditionalGetEtag.JsonSerializerOptions);
        second.ShouldNotBeNull();

        second!.TotalRequestsServed.ShouldBeGreaterThan(first!.TotalRequestsServed);
    }

    [Test]
    public async Task Should_EnforceApiKey_When_MiddlewareEnabledAndMetricsProtected()
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

    private static void AssertRequiredProperties(JsonElement root)
    {
        root.TryGetProperty("uptime", out _).ShouldBeTrue();
        root.TryGetProperty("totalRequestsServed", out _).ShouldBeTrue();
        root.TryGetProperty("workingSetMb", out _).ShouldBeTrue();
        root.TryGetProperty("gcMemoryMb", out _).ShouldBeTrue();
        root.TryGetProperty("gcCollectionCounts", out var gcCounts).ShouldBeTrue();
        gcCounts.TryGetProperty("gen0", out _).ShouldBeTrue();
        gcCounts.TryGetProperty("gen1", out _).ShouldBeTrue();
        gcCounts.TryGetProperty("gen2", out _).ShouldBeTrue();
        root.TryGetProperty("capturedAtUtc", out _).ShouldBeTrue();
    }
}
