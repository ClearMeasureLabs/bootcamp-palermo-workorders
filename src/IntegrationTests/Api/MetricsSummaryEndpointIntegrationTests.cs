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
    public async Task Should_IncreaseTotalRequests_When_AdditionalGetsAfterSummary()
    {
        var first = await _client!.GetAsync("/api/metrics/summary");
        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        var firstPayload = await first.Content.ReadFromJsonAsync<MetricsSummaryResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        firstPayload.ShouldNotBeNull();
        var baseline = firstPayload!.TotalRequestsServed;

        for (var i = 0; i < 4; i++)
        {
            (await _client.GetAsync("/api/version")).StatusCode.ShouldBe(HttpStatusCode.OK);
        }

        var second = await _client.GetAsync("/api/metrics/summary");
        second.StatusCode.ShouldBe(HttpStatusCode.OK);
        var secondPayload = await second.Content.ReadFromJsonAsync<MetricsSummaryResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        secondPayload.ShouldNotBeNull();
        secondPayload!.TotalRequestsServed.ShouldBeGreaterThanOrEqualTo(baseline + 5);
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

    private static void AssertRequiredProperties(JsonElement root)
    {
        root.TryGetProperty("uptime", out _).ShouldBeTrue();
        root.TryGetProperty("totalRequestsServed", out _).ShouldBeTrue();
        root.TryGetProperty("workingSetBytes", out _).ShouldBeTrue();
        root.TryGetProperty("totalAllocatedBytes", out _).ShouldBeTrue();
        root.TryGetProperty("gcGen0Collections", out _).ShouldBeTrue();
        root.TryGetProperty("gcGen1Collections", out _).ShouldBeTrue();
        root.TryGetProperty("gcGen2Collections", out _).ShouldBeTrue();
    }
}
