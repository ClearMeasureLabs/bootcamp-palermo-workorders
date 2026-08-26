using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UnitTests.UI.Server;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

/// <summary>
/// Integration coverage for <c>GET /api/metrics/summary</c> and versioned route (issue #9158).
/// </summary>
[TestFixture]
public class MetricsSummaryEndpointIntegrationTests
{
    private DetailedHealthWebApplicationFactory? _factory;
    private HttpClient? _client;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _factory = new DetailedHealthWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    [Test]
    public async Task Should_Return200AndJson_When_GetMetricsSummary()
    {
        var response = await _client!.GetAsync("/api/metrics/summary");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType.ShouldContain("application/json");

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        AssertRequiredProperties(doc.RootElement);
    }

    [Test]
    public async Task Should_Return200_When_GetVersionedMetricsSummary()
    {
        var response = await _client!.GetAsync("/api/v1.0/metrics/summary");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task Should_IncreaseTotalRequestsServed_When_AdditionalRequestMade()
    {
        var first = await _client!.GetAsync("/api/metrics/summary");
        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        var firstTotal = await ReadTotalAsync(first);

        await _client.GetAsync("/api/ping");

        var second = await _client.GetAsync("/api/metrics/summary");
        second.StatusCode.ShouldBe(HttpStatusCode.OK);
        var secondTotal = await ReadTotalAsync(second);

        secondTotal.ShouldBeGreaterThan(firstTotal);
    }

    [Test]
    public async Task Should_IncludeWeakEtag_When_GetMetricsSummary()
    {
        var response = await _client!.GetAsync("/api/metrics/summary");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.ETag.ShouldNotBeNull();
        response.Headers.ETag!.IsWeak.ShouldBeTrue();
    }

    [Test]
    public async Task Should_Return304_When_IfNoneMatchIsAny()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/metrics/summary");
        request.Headers.IfNoneMatch.Add(EntityTagHeaderValue.Any);

        var response = await _client!.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.NotModified);
        (await response.Content.ReadAsByteArrayAsync()).Length.ShouldBe(0);
        response.Headers.ETag.ShouldNotBeNull();
    }

    [Test]
    public async Task Should_EnforceApiKey_When_MiddlewareEnabledAndMetricsProtected()
    {
        await using var factory = new ApiKeyProtectedWebApplicationFactory();
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
        root.TryGetProperty("totalRequestsServed", out var total).ShouldBeTrue();
        total.ValueKind.ShouldBe(JsonValueKind.Number);
        total.GetInt64().ShouldBeGreaterThanOrEqualTo(0);
        root.TryGetProperty("workingSetBytes", out var workingSet).ShouldBeTrue();
        workingSet.GetInt64().ShouldBeGreaterThan(0);
        root.TryGetProperty("managedMemoryBytes", out var managed).ShouldBeTrue();
        managed.GetInt64().ShouldBeGreaterThanOrEqualTo(0);
        root.TryGetProperty("gcGen0Collections", out _).ShouldBeTrue();
        root.TryGetProperty("gcGen1Collections", out _).ShouldBeTrue();
        root.TryGetProperty("gcGen2Collections", out _).ShouldBeTrue();
    }

    private static async Task<long> ReadTotalAsync(HttpResponseMessage response)
    {
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        return doc.RootElement.GetProperty("totalRequestsServed").GetInt64();
    }
}
