using System.Net;
using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Shared;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

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
        _client.DefaultRequestHeaders.Add(
            ApiKeyConstants.HeaderName,
            DetailedHealthWebApplicationFactory.IntegrationApiKey);
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
