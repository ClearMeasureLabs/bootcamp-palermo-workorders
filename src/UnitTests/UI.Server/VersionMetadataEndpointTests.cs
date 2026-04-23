using System.Globalization;
using System.Net;
using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Server.RateLimiting;
using ClearMeasure.Bootcamp.UnitTests.Api;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class VersionMetadataEndpointTests
{
    private static IReadOnlyDictionary<string, string?> StrictVersionLimitSettings() =>
        new Dictionary<string, string?>
        {
            ["ApiRateLimiting:Enabled"] = "true",
            ["ApiRateLimiting:PermitLimit"] = "1",
            ["ApiRateLimiting:WindowSeconds"] = "2",
            ["ApiRateLimiting:SegmentsPerWindow"] = "2",
            ["ApiRateLimiting:QueueLimit"] = "0",
            ["ApiRateLimiting:ApiKeyHeaderName"] = "X-API-Key"
        };

    [Test]
    public async Task Should_Return200AndJsonContract_When_GetVersionUnversioned()
    {
        await using var factory = new ApiVersioningRoutingWebApplicationFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/version");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = doc.RootElement;
        root.GetProperty("assemblyVersion").GetString().ShouldNotBeNullOrEmpty();
        root.GetProperty("informationalVersion").GetString().ShouldNotBeNullOrEmpty();
        root.GetProperty("configuration").GetString().ShouldNotBeNullOrEmpty();
        root.GetProperty("environment").GetString().ShouldBe("Testing");
        root.GetProperty("machineName").GetString().ShouldNotBeNullOrEmpty();
        root.GetProperty("frameworkDescription").GetString().ShouldNotBeNullOrEmpty();
    }

    [Test]
    public async Task Should_Return200AndMatchingMetadata_When_GetVersionVersionedAndUnversioned()
    {
        await using var factory = new ApiVersioningRoutingWebApplicationFactory();
        using var client = factory.CreateClient();

        using var unversioned = await client.GetAsync("/api/version");
        using var versioned = await client.GetAsync("/api/v1.0/version");

        unversioned.StatusCode.ShouldBe(HttpStatusCode.OK);
        versioned.StatusCode.ShouldBe(HttpStatusCode.OK);

        var a = await unversioned.Content.ReadAsStringAsync();
        var b = await versioned.Content.ReadAsStringAsync();
        using var docA = JsonDocument.Parse(a);
        using var docB = JsonDocument.Parse(b);

        foreach (var name in new[]
                 {
                     "assemblyVersion", "informationalVersion", "configuration", "environment", "machineName",
                     "frameworkDescription"
                 })
        {
            docA.RootElement.GetProperty(name).GetRawText().ShouldBe(docB.RootElement.GetProperty(name).GetRawText());
        }
    }

    [Test]
    public async Task Should_IndicateOutputCache_When_SecondRequestWithinPolicyWindow()
    {
        await using var factory = new ApiVersioningRoutingWebApplicationFactory();
        using var client = factory.CreateClient();

        using var first = await client.GetAsync("/api/version");
        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        first.Headers.Age.ShouldBeNull();

        using var second = await client.GetAsync("/api/version");
        second.StatusCode.ShouldBe(HttpStatusCode.OK);
        second.Headers.Age.ShouldNotBeNull();
        second.Headers.Age!.Value.TotalSeconds.ShouldBeGreaterThanOrEqualTo(0);
    }

    [Test]
    public async Task Should_EnforceRateLimitPolicy_When_RepeatedGetsToVersionUnderTightLimiter()
    {
        await using var factory = new TunableApiRateLimitWebApplicationFactory(StrictVersionLimitSettings());
        using var client = factory.CreateClient();

        (await client.GetAsync("/api/version")).StatusCode.ShouldBe(HttpStatusCode.OK);

        var limited = await client.GetAsync("/api/version");
        limited.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        limited.Headers.TryGetValues("Retry-After", out var ra).ShouldBeTrue();
        ra!.First().ShouldBe("2");
        limited.Content.Headers.ContentType?.MediaType.ShouldBe("text/plain");
        (await limited.Content.ReadAsStringAsync()).ShouldBe("Too many requests. Please try again later.");
    }

    [Test]
    public async Task Should_ExposeRateLimitHeaders_When_GetVersionUnderTunableLimiter()
    {
        var settings = new Dictionary<string, string?>
        {
            ["ApiRateLimiting:Enabled"] = "true",
            ["ApiRateLimiting:PermitLimit"] = "5",
            ["ApiRateLimiting:WindowSeconds"] = "2",
            ["ApiRateLimiting:SegmentsPerWindow"] = "2",
            ["ApiRateLimiting:QueueLimit"] = "0",
            ["ApiRateLimiting:ApiKeyHeaderName"] = "X-API-Key"
        };
        await using var factory = new TunableApiRateLimitWebApplicationFactory(settings);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/version");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.TryGetValues(RateLimitingMiddleware.HeaderLimit, out var limit).ShouldBeTrue();
        response.Headers.TryGetValues(RateLimitingMiddleware.HeaderRemaining, out var remaining).ShouldBeTrue();
        limit!.First().ShouldBe("5");
        int.Parse(remaining!.First(), NumberFormatInfo.InvariantInfo).ShouldBeGreaterThanOrEqualTo(0);
    }
}
