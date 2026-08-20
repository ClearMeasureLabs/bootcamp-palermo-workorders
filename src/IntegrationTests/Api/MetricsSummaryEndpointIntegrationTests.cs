using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
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
        doc.RootElement.TryGetProperty("gcMemoryMb", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("workingSetMb", out _).ShouldBeTrue();
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
        doc.RootElement.TryGetProperty("uptime", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("totalRequestsServed", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("gcMemoryMb", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("workingSetMb", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("gcCollectionCounts", out _).ShouldBeTrue();
    }

    [Test]
    public async Task Should_ExposeNonNegativeUptimeAndMemory_When_GetMetricsSummary()
    {
        var response = await _client!.GetAsync("/api/metrics/summary");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<MetricsSummaryResponse>(
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload!.Uptime.ShouldBeGreaterThanOrEqualTo(TimeSpan.Zero);
        payload.GcMemoryMb.ShouldBeGreaterThanOrEqualTo(0);
        payload.WorkingSetMb.ShouldBeGreaterThanOrEqualTo(0);
    }

    [Test]
    public async Task Should_IncreaseTotalRequestsServed_AcrossSequentialCalls()
    {
        var firstResponse = await _client!.GetAsync("/api/metrics/summary");
        firstResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var firstPayload = await firstResponse.Content.ReadFromJsonAsync<MetricsSummaryResponse>(
            ConditionalGetEtag.JsonSerializerOptions);
        firstPayload.ShouldNotBeNull();

        var secondResponse = await _client.GetAsync("/api/metrics/summary");
        secondResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var secondPayload = await secondResponse.Content.ReadFromJsonAsync<MetricsSummaryResponse>(
            ConditionalGetEtag.JsonSerializerOptions);
        secondPayload.ShouldNotBeNull();

        secondPayload!.TotalRequestsServed.ShouldBeGreaterThan(firstPayload!.TotalRequestsServed);
    }

    [Test]
    public async Task Should_Return200WithoutApiKey_When_MiddlewareEnabled()
    {
        await using var factory = new ApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var unversioned = await client.GetAsync("/api/metrics/summary");
        unversioned.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await unversioned.Content.ReadAsStringAsync()).ShouldNotBeNullOrWhiteSpace();

        var versioned = await client.GetAsync("/api/v1.0/metrics/summary");
        versioned.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await versioned.Content.ReadAsStringAsync()).ShouldNotBeNullOrWhiteSpace();
    }
}
