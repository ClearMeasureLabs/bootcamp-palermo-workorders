using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UnitTests.UI.Server;
using ClearMeasure.Bootcamp.UI.Shared;
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
    public async Task Should_Return200AndJson_WithExpectedTopLevelProperties_When_GetMetricsSummaryUnversioned()
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
        doc.RootElement.TryGetProperty("workingSetBytes", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("gcTotalMemoryBytes", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("gcCollections", out var gc).ShouldBeTrue();
        gc.TryGetProperty("gen0", out _).ShouldBeTrue();
        gc.TryGetProperty("gen1", out _).ShouldBeTrue();
        gc.TryGetProperty("gen2", out _).ShouldBeTrue();
    }

    [Test]
    public async Task Should_Return200AndJson_WithExpectedTopLevelProperties_When_GetMetricsSummaryVersioned()
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
    }

    [Test]
    public async Task Should_AlignUptime_WithSimpleHealthSemantics_When_GetMetricsSummary()
    {
        var before = DateTime.UtcNow;
        var metricsResponse = await _client!.GetAsync("/api/metrics/summary");
        var after = DateTime.UtcNow;
        metricsResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var payload = await metricsResponse.Content.ReadFromJsonAsync<MetricsSummaryResponse>(
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        var health = SimpleHealthResponseBuilder.Build(TimeProvider.System);
        (after - before).ShouldBeLessThan(TimeSpan.FromSeconds(30));
        payload!.Uptime.ShouldBeLessThanOrEqualTo(health.Uptime + TimeSpan.FromSeconds(2));
        payload.Uptime.ShouldBeGreaterThanOrEqualTo(health.Uptime - TimeSpan.FromSeconds(2));
    }

    [Test]
    public async Task Should_ExposeNonNegativeNumericMetrics_When_GetMetricsSummary()
    {
        var response = await _client!.GetAsync("/api/metrics/summary");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<MetricsSummaryResponse>(
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload!.TotalRequestsServed.ShouldBeGreaterThanOrEqualTo(0);
        payload.WorkingSetBytes.ShouldBeGreaterThanOrEqualTo(0);
        payload.GcTotalMemoryBytes.ShouldBeGreaterThanOrEqualTo(0);
        payload.GcCollections.Gen0.ShouldBeGreaterThanOrEqualTo(0);
        payload.GcCollections.Gen1.ShouldBeGreaterThanOrEqualTo(0);
        payload.GcCollections.Gen2.ShouldBeGreaterThanOrEqualTo(0);
    }

    [Test]
    public async Task Should_IncrementRequestCounter_ForRequestsMatchingDocumentedScope_When_MultipleGets()
    {
        await using var factory = new DiagnosticsWebApplicationFactory();
        using var client = factory.CreateClient();

        var first = await client.GetAsync("/api/metrics/summary");
        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        var firstPayload = await first.Content.ReadFromJsonAsync<MetricsSummaryResponse>(
            ConditionalGetEtag.JsonSerializerOptions);
        firstPayload.ShouldNotBeNull();

        await client.GetAsync("/api/metrics/summary");
        await client.GetAsync("/api/metrics/summary");

        var last = await client.GetAsync("/api/metrics/summary");
        last.StatusCode.ShouldBe(HttpStatusCode.OK);
        var lastPayload = await last.Content.ReadFromJsonAsync<MetricsSummaryResponse>(
            ConditionalGetEtag.JsonSerializerOptions);
        lastPayload.ShouldNotBeNull();

        lastPayload!.TotalRequestsServed.ShouldBeGreaterThan(firstPayload!.TotalRequestsServed);
        (lastPayload.TotalRequestsServed - firstPayload.TotalRequestsServed).ShouldBeGreaterThanOrEqualTo(3);
    }

    [Test]
    public async Task Should_Return401_When_ApiKeyRequiredAndHeaderMissing()
    {
        await using var factory = new DiagnosticsApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var unauth = await client.GetAsync("/api/metrics/summary");
        unauth.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        var unauthVersioned = await client.GetAsync("/api/v1.0/metrics/summary");
        unauthVersioned.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Should_Return200_When_ApiKeyProvidedAndRequired()
    {
        await using var factory = new DiagnosticsApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(
            ApiKeyConstants.HeaderName,
            ApiKeyProtectedWebApplicationFactory.TestApiKey);

        var ok = await client.GetAsync("/api/metrics/summary");
        ok.StatusCode.ShouldBe(HttpStatusCode.OK);

        var okVersioned = await client.GetAsync("/api/v1.0/metrics/summary");
        okVersioned.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task Should_Return304Or200ConsistentWithConditionalGetEtag_When_IfNoneMatch()
    {
        var response = await _client!.GetAsync("/api/metrics/summary");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var etagValues = response.Headers.ETag;
        etagValues.ShouldNotBeNull();
        var etag = etagValues!.ToString();
        etag.ShouldNotBeNullOrWhiteSpace();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/metrics/summary");
        request.Headers.IfNoneMatch.ParseAdd(etag);
        var conditional = await _client.SendAsync(request);
        conditional.StatusCode.ShouldBeOneOf(HttpStatusCode.NotModified, HttpStatusCode.OK);
    }
}
