using System.Net;
using System.Text.Json;
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
    public async Task Should_AllowAnonymous_When_GetMetricsSummary()
    {
        await using var factory = new ApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var unversioned = await client.GetAsync("/api/metrics/summary");
        unversioned.StatusCode.ShouldBe(HttpStatusCode.OK);

        var versioned = await client.GetAsync("/api/v1.0/metrics/summary");
        versioned.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task Should_ReflectRequestCountIncrease_When_EndpointCalledTwice()
    {
        await using var factory = new DiagnosticsWebApplicationFactory();
        using var client = factory.CreateClient();

        var first = await client.GetAsync("/api/metrics/summary");
        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        var firstCount = await ReadTotalRequestsAsync(first);

        var second = await client.GetAsync("/api/metrics/summary");
        second.StatusCode.ShouldBe(HttpStatusCode.OK);
        var secondCount = await ReadTotalRequestsAsync(second);

        secondCount.ShouldBeGreaterThan(firstCount);
    }

    private static void AssertRequiredProperties(JsonElement root)
    {
        root.TryGetProperty("uptime", out _).ShouldBeTrue();
        root.TryGetProperty("totalRequestsServed", out _).ShouldBeTrue();
        root.TryGetProperty("memory", out var memory).ShouldBeTrue();
        memory.TryGetProperty("gcMemoryBytes", out _).ShouldBeTrue();
        memory.TryGetProperty("workingSetBytes", out _).ShouldBeTrue();
        root.TryGetProperty("gcCollections", out var gc).ShouldBeTrue();
        gc.TryGetProperty("gen0", out _).ShouldBeTrue();
        gc.TryGetProperty("gen1", out _).ShouldBeTrue();
        gc.TryGetProperty("gen2", out _).ShouldBeTrue();
    }

    private static async Task<long> ReadTotalRequestsAsync(HttpResponseMessage response)
    {
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        return doc.RootElement.GetProperty("totalRequestsServed").GetInt64();
    }
}
