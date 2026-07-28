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
    public async Task Should_Return200AndJson_WithExpectedProperties_When_GetMetricsSummaryUnversioned()
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
        doc.RootElement.TryGetProperty("managedMemoryBytes", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("gcGen0Collections", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("gcGen1Collections", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("gcGen2Collections", out _).ShouldBeTrue();
    }

    [Test]
    public async Task Should_Return200AndJson_WithExpectedProperties_When_GetMetricsSummaryVersioned()
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
        doc.RootElement.TryGetProperty("managedMemoryBytes", out _).ShouldBeTrue();
    }

    [Test]
    public async Task Should_IncreaseTotalRequestsAcrossSequentialCalls_When_MetricsEndpointHitMultipleTimes()
    {
        var first = await _client!.GetAsync("/api/metrics/summary");
        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        var firstPayload = await first.Content.ReadFromJsonAsync<MetricsSummaryResponse>(
            ConditionalGetEtag.JsonSerializerOptions);
        firstPayload.ShouldNotBeNull();

        var second = await _client.GetAsync("/api/metrics/summary");
        second.StatusCode.ShouldBe(HttpStatusCode.OK);
        var secondPayload = await second.Content.ReadFromJsonAsync<MetricsSummaryResponse>(
            ConditionalGetEtag.JsonSerializerOptions);
        secondPayload.ShouldNotBeNull();

        secondPayload!.TotalRequests.ShouldBeGreaterThan(firstPayload!.TotalRequests);
    }

    [Test]
    public async Task Should_EnforceApiKey_When_MiddlewareEnabledAndRouteNotPublic()
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
    public async Task Should_StayConsistentWithDiagnostics_When_ComparingUptimeBounds()
    {
        var diagnostics = await _client!.GetAsync("/api/diagnostics");
        diagnostics.StatusCode.ShouldBe(HttpStatusCode.OK);

        var metrics = await _client.GetAsync("/api/metrics/summary");
        metrics.StatusCode.ShouldBe(HttpStatusCode.OK);

        var diagnosticsUptime =
            JsonSerializer.Deserialize<DiagnosticsResponse>(
                await diagnostics.Content.ReadAsStringAsync(),
                ConditionalGetEtag.JsonSerializerOptions)!.Uptime;
        var metricsUptime =
            JsonSerializer.Deserialize<MetricsSummaryResponse>(
                await metrics.Content.ReadAsStringAsync(),
                ConditionalGetEtag.JsonSerializerOptions)!.Uptime;

        var skew = diagnosticsUptime - metricsUptime;
        Math.Abs(skew.TotalMilliseconds).ShouldBeLessThan(2000);
    }
}
