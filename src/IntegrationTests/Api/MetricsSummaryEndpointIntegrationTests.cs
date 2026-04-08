using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UnitTests.Api;
using ClearMeasure.Bootcamp.UnitTests.UI.Server;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

[TestFixture]
public class MetricsSummaryEndpointIntegrationTests
{
    [Test]
    public async Task Should_Return200AndJson_When_GetMetricsSummary()
    {
        await using var factory = new DetailedHealthWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/metrics/summary");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");

        await AssertMetricsContractAsync(response);
    }

    [Test]
    public async Task Should_ExposeSameContractOnLegacyAndV1Paths_When_VersionedRouting()
    {
        await using var factory = new DetailedHealthWebApplicationFactory();
        using var client = factory.CreateClient();

        var legacy = await client.GetAsync("/api/metrics/summary");
        var v1 = await client.GetAsync("/api/v1.0/metrics/summary");

        legacy.StatusCode.ShouldBe(HttpStatusCode.OK);
        v1.StatusCode.ShouldBe(HttpStatusCode.OK);

        v1.Headers.TryGetValues("api-supported-versions", out var supported).ShouldBeTrue();
        string.Join(", ", supported!).ShouldContain("1.0");

        var legacyText = await legacy.Content.ReadAsStringAsync();
        var v1Text = await v1.Content.ReadAsStringAsync();
        using var legacyDoc = JsonDocument.Parse(legacyText);
        using var v1Doc = JsonDocument.Parse(v1Text);
        MetricsJsonAssert.SameShapeAndAlignedCounters(legacyDoc.RootElement, v1Doc.RootElement);
    }

    [Test]
    public async Task Should_AllowAnonymousAccess_When_ApiKeyDisabled()
    {
        await using var factory = new DetailedHealthWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/metrics/summary");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task Should_EnforceApiKey_When_RequiredAndMissingOrInvalid()
    {
        await using var factory = new ApiKeyProtectedWebApplicationFactory();
        using var noKey = factory.CreateClient();

        (await noKey.GetAsync("/api/metrics/summary")).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        using var wrongKey = factory.CreateClient();
        wrongKey.DefaultRequestHeaders.Add(ApiKeyConstants.HeaderName, "wrong");
        (await wrongKey.GetAsync("/api/v1.0/metrics/summary")).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        using var ok = factory.CreateClient();
        ok.DefaultRequestHeaders.Add(ApiKeyConstants.HeaderName, ApiKeyProtectedWebApplicationFactory.TestApiKey);
        (await ok.GetAsync("/api/metrics/summary")).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task Should_ApplyRateLimiting_When_EndpointUsesApiLimiter()
    {
        var settings = new Dictionary<string, string?>
        {
            ["ApiRateLimiting:Enabled"] = "true",
            ["ApiRateLimiting:PermitLimit"] = "2",
            ["ApiRateLimiting:WindowSeconds"] = "60",
            ["ApiRateLimiting:SegmentsPerWindow"] = "2",
            ["ApiRateLimiting:QueueLimit"] = "0",
            ["ApiRateLimiting:ApiKeyHeaderName"] = "X-API-Key"
        };
        await using var factory = new TunableApiRateLimitWebApplicationFactory(settings);
        using var client = factory.CreateClient();

        (await client.GetAsync("/api/metrics/summary")).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await client.GetAsync("/api/metrics/summary")).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await client.GetAsync("/api/metrics/summary")).StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
    }

    [Test]
    public async Task Should_ReturnError_When_UnsupportedApiVersion()
    {
        await using var factory = new DetailedHealthWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v2.0/metrics/summary");

        response.IsSuccessStatusCode.ShouldBeFalse();
        response.StatusCode.ShouldBeOneOf(HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Should_ExposeSaneBoundedValues_When_ProcessRunning()
    {
        await using var factory = new DetailedHealthWebApplicationFactory();
        using var client = factory.CreateClient();

        var first = await client.GetAsync("/api/metrics/summary");
        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        var firstPayload = await first.Content.ReadFromJsonAsync<MetricsSummaryResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        firstPayload.ShouldNotBeNull();
        AssertSaneMetrics(firstPayload!);

        await client.GetAsync("/api/health");

        var second = await client.GetAsync("/api/metrics/summary");
        second.StatusCode.ShouldBe(HttpStatusCode.OK);
        var secondPayload = await second.Content.ReadFromJsonAsync<MetricsSummaryResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        secondPayload.ShouldNotBeNull();
        AssertSaneMetrics(secondPayload!);
        secondPayload!.TotalRequestsServed.ShouldBeGreaterThanOrEqualTo(firstPayload!.TotalRequestsServed + 1);
    }

    private static void AssertSaneMetrics(MetricsSummaryResponse m)
    {
        m.Uptime.ShouldBeGreaterThanOrEqualTo(TimeSpan.Zero);
        m.TotalRequestsServed.ShouldBeGreaterThanOrEqualTo(0);
        m.WorkingSetBytes.ShouldBeGreaterThanOrEqualTo(0);
        m.GcGen0Collections.ShouldBeGreaterThanOrEqualTo(0);
        m.GcGen1Collections.ShouldBeGreaterThanOrEqualTo(0);
        m.GcGen2Collections.ShouldBeGreaterThanOrEqualTo(0);
    }

    private static async Task AssertMetricsContractAsync(HttpResponseMessage response)
    {
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        var root = doc.RootElement;
        root.TryGetProperty("uptime", out _).ShouldBeTrue();
        root.TryGetProperty("totalRequestsServed", out _).ShouldBeTrue();
        root.GetProperty("totalRequestsServed").GetInt64().ShouldBeGreaterThanOrEqualTo(0);
        root.TryGetProperty("workingSetBytes", out _).ShouldBeTrue();
        root.GetProperty("workingSetBytes").GetInt64().ShouldBeGreaterThanOrEqualTo(0);
        root.TryGetProperty("gcGen0Collections", out _).ShouldBeTrue();
        root.TryGetProperty("gcGen1Collections", out _).ShouldBeTrue();
        root.TryGetProperty("gcGen2Collections", out _).ShouldBeTrue();
    }

    private static class MetricsJsonAssert
    {
        /// <summary>
        /// Same JSON shape and value kinds; <c>totalRequestsServed</c> may differ by one (snapshot before increment).
        /// Memory and GC fields are not compared for equality—process state can change between back-to-back GETs.
        /// </summary>
        public static void SameShapeAndAlignedCounters(JsonElement a, JsonElement b)
        {
            var totalA = a.GetProperty("totalRequestsServed").GetInt64();
            var totalB = b.GetProperty("totalRequestsServed").GetInt64();
            totalA.ShouldBeGreaterThanOrEqualTo(0);
            totalB.ShouldBeGreaterThanOrEqualTo(0);
            Math.Abs(totalA - totalB).ShouldBeLessThanOrEqualTo(1);

            a.GetProperty("workingSetBytes").ValueKind.ShouldBe(JsonValueKind.Number);
            b.GetProperty("workingSetBytes").ValueKind.ShouldBe(JsonValueKind.Number);
            a.GetProperty("workingSetBytes").GetInt64().ShouldBeGreaterThanOrEqualTo(0);
            b.GetProperty("workingSetBytes").GetInt64().ShouldBeGreaterThanOrEqualTo(0);

            foreach (var name in new[] { "gcGen0Collections", "gcGen1Collections", "gcGen2Collections" })
            {
                a.GetProperty(name).ValueKind.ShouldBe(JsonValueKind.Number);
                b.GetProperty(name).ValueKind.ShouldBe(JsonValueKind.Number);
                a.GetProperty(name).GetInt32().ShouldBeGreaterThanOrEqualTo(0);
                b.GetProperty(name).GetInt32().ShouldBeGreaterThanOrEqualTo(0);
            }

            var uptimeA = a.GetProperty("uptime").GetString();
            var uptimeB = b.GetProperty("uptime").GetString();
            uptimeA.ShouldNotBeNull();
            uptimeB.ShouldNotBeNull();
            TimeSpan.Parse(uptimeA!, CultureInfo.InvariantCulture);
            TimeSpan.Parse(uptimeB!, CultureInfo.InvariantCulture);
        }
    }
}
