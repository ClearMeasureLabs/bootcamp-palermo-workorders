using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using ClearMeasure.Bootcamp.UI.Server.RateLimiting;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UnitTests.Api;
using ClearMeasure.Bootcamp.UnitTests.UI.Server;
using Microsoft.AspNetCore.Mvc.Testing;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

[TestFixture]
public class GuidGeneratorEndpointIntegrationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    [Test]
    public async Task Should_Return200AndSingleGuid_When_PostWithoutBodyOrDefaultCount()
    {
        await using var factory = new DiagnosticsWebApplicationFactory();
        using var client = factory.CreateClient();

        using var emptyBody = new HttpRequestMessage(HttpMethod.Post, "/api/tools/guid-generator");
        emptyBody.Content = new StringContent("", Encoding.UTF8, "application/json");
        var noBody = await client.SendAsync(emptyBody);
        noBody.StatusCode.ShouldBe(HttpStatusCode.OK);
        await AssertSingleValidGuidAsync(noBody);

        var withEmptyJson = await client.PostAsync("/api/tools/guid-generator", new StringContent("{}", Encoding.UTF8, "application/json"));
        withEmptyJson.StatusCode.ShouldBe(HttpStatusCode.OK);
        await AssertSingleValidGuidAsync(withEmptyJson);
    }

    [Test]
    public async Task Should_Return200AndDistinctGuids_When_CountWithinRange()
    {
        await using var factory = new DiagnosticsWebApplicationFactory();
        using var client = factory.CreateClient();

        foreach (var count in new[] { 2, 100 })
        {
            var response = await client.PostAsync(
                "/api/tools/guid-generator",
                JsonContent.Create(new GuidGeneratorRequest { Count = count }));
            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            var payload = await response.Content.ReadFromJsonAsync<GuidGeneratorResponse>(JsonOptions);
            payload.ShouldNotBeNull();
            payload!.Guids.Count.ShouldBe(count);
            var set = new HashSet<string>(StringComparer.Ordinal);
            foreach (var g in payload.Guids)
            {
                Guid.TryParse(g, out _).ShouldBeTrue();
                set.Add(g).ShouldBeTrue();
            }
        }
    }

    [Test]
    public async Task Should_Return400_When_CountOutOfRange()
    {
        await using var factory = new DiagnosticsWebApplicationFactory();
        using var client = factory.CreateClient();

        foreach (var count in new int?[] { 0, 101, -1 })
        {
            var response = await client.PostAsync(
                "/api/tools/guid-generator",
                JsonContent.Create(new GuidGeneratorRequest { Count = count }));
            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            var mediaType = response.Content.Headers.ContentType?.MediaType;
            mediaType.ShouldNotBeNull();
            mediaType!.ShouldContain("json");
        }

        var nonNumeric = await client.PostAsync(
            "/api/tools/guid-generator",
            new StringContent("{\"count\":\"not-a-number\"}", Encoding.UTF8, "application/json"));
        nonNumeric.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Should_Return401_When_ApiKeyRequiredAndMissingOrWrong()
    {
        await using var factory = new DiagnosticsApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var missing = await client.PostAsync(
            "/api/tools/guid-generator",
            new StringContent("{}", Encoding.UTF8, "application/json"));
        missing.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        using var wrongKey = factory.CreateClient();
        wrongKey.DefaultRequestHeaders.Add(ApiKeyConstants.HeaderName, "wrong-key");
        var wrong = await wrongKey.PostAsync(
            "/api/tools/guid-generator",
            new StringContent("{}", Encoding.UTF8, "application/json"));
        wrong.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        using var okClient = factory.CreateClient();
        okClient.DefaultRequestHeaders.Add(ApiKeyConstants.HeaderName, ApiKeyProtectedWebApplicationFactory.TestApiKey);
        var ok = await okClient.PostAsync(
            "/api/tools/guid-generator",
            new StringContent("{}", Encoding.UTF8, "application/json"));
        ok.StatusCode.ShouldBe(HttpStatusCode.OK);
        await AssertSingleValidGuidAsync(ok);

        var okVersioned = await okClient.PostAsync(
            "/api/v1.0/tools/guid-generator",
            new StringContent("{}", Encoding.UTF8, "application/json"));
        okVersioned.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task Should_RejectNonPost_When_GetOrPut()
    {
        await using var factory = new DiagnosticsWebApplicationFactory();
        using var client = factory.CreateClient();

        (await client.GetAsync("/api/tools/guid-generator")).StatusCode.ShouldBe(HttpStatusCode.MethodNotAllowed);
        (await client.PutAsync("/api/tools/guid-generator", new StringContent("{}", Encoding.UTF8, "application/json")))
            .StatusCode.ShouldBe(HttpStatusCode.MethodNotAllowed);

        (await client.GetAsync("/api/v1.0/tools/guid-generator")).StatusCode.ShouldBe(HttpStatusCode.MethodNotAllowed);
    }

    [Test]
    public async Task Should_HonorIdempotency_When_IdempotencyKeyHeaderPresent()
    {
        await using var factory = new DiagnosticsWebApplicationFactory();
        using var client = factory.CreateClient();

        const string idemKey = "guid-gen-idem-5570";
        using var req1 = new HttpRequestMessage(HttpMethod.Post, "/api/tools/guid-generator");
        req1.Headers.Add(IdempotencyConstants.HeaderName, idemKey);
        req1.Content = new StringContent("{\"count\":3}", Encoding.UTF8, "application/json");

        var r1 = await client.SendAsync(req1);
        r1.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body1 = await r1.Content.ReadAsByteArrayAsync();

        using var req2 = new HttpRequestMessage(HttpMethod.Post, "/api/tools/guid-generator");
        req2.Headers.Add(IdempotencyConstants.HeaderName, idemKey);
        req2.Content = new StringContent("{\"count\":3}", Encoding.UTF8, "application/json");
        var r2 = await client.SendAsync(req2);
        r2.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body2 = await r2.Content.ReadAsByteArrayAsync();
        body2.ShouldBe(body1);

        using var reqConflict = new HttpRequestMessage(HttpMethod.Post, "/api/tools/guid-generator");
        reqConflict.Headers.Add(IdempotencyConstants.HeaderName, idemKey);
        reqConflict.Content = new StringContent("{\"count\":2}", Encoding.UTF8, "application/json");
        var rConflict = await client.SendAsync(reqConflict);
        rConflict.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Test]
    public async Task Should_ApplyRateLimiting_When_VersionedRouteUsesEnableRateLimiting()
    {
        var overrides = new Dictionary<string, string?>
        {
            ["ApiRateLimiting:Enabled"] = "true",
            ["ApiRateLimiting:PermitLimit"] = "1",
            ["ApiRateLimiting:WindowSeconds"] = "2",
            ["ApiRateLimiting:SegmentsPerWindow"] = "2",
            ["ApiRateLimiting:QueueLimit"] = "0",
            ["ApiRateLimiting:ApiKeyHeaderName"] = "X-API-Key"
        };
        await using var factory = new TunableApiRateLimitWebApplicationFactory(overrides);
        using var client = factory.CreateClient();

        var first = await client.PostAsync(
            "/api/v1.0/tools/guid-generator",
            new StringContent("{}", Encoding.UTF8, "application/json"));
        first.StatusCode.ShouldBe(HttpStatusCode.OK);

        var second = await client.PostAsync(
            "/api/v1.0/tools/guid-generator",
            new StringContent("{}", Encoding.UTF8, "application/json"));
        second.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        second.Headers.TryGetValues(RateLimitingMiddleware.HeaderLimit, out var limit).ShouldBeTrue();
        limit!.First().ShouldBe("1");
    }

    [Test]
    public async Task Should_ReturnUniqueGuidsAcrossSequentialCalls_When_CountOne()
    {
        await using var factory = new DiagnosticsWebApplicationFactory();
        using var client = factory.CreateClient();

        var r1 = await client.PostAsync("/api/tools/guid-generator", new StringContent("{}", Encoding.UTF8, "application/json"));
        var r2 = await client.PostAsync("/api/tools/guid-generator", new StringContent("{}", Encoding.UTF8, "application/json"));
        r1.StatusCode.ShouldBe(HttpStatusCode.OK);
        r2.StatusCode.ShouldBe(HttpStatusCode.OK);

        var p1 = await r1.Content.ReadFromJsonAsync<GuidGeneratorResponse>(JsonOptions);
        var p2 = await r2.Content.ReadFromJsonAsync<GuidGeneratorResponse>(JsonOptions);
        p1.ShouldNotBeNull();
        p2.ShouldNotBeNull();
        p1!.Guids[0].ShouldNotBe(p2!.Guids[0]);
    }

    private static async Task AssertSingleValidGuidAsync(HttpResponseMessage response)
    {
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");
        var payload = await response.Content.ReadFromJsonAsync<GuidGeneratorResponse>(JsonOptions);
        payload.ShouldNotBeNull();
        payload!.Guids.Count.ShouldBe(1);
        Guid.TryParse(payload.Guids[0], out _).ShouldBeTrue();
    }
}
