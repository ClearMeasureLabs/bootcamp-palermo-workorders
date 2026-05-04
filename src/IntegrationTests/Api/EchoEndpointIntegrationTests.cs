using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UnitTests.Api;
using ClearMeasure.Bootcamp.UnitTests.UI.Server;
using Microsoft.AspNetCore.Mvc.Testing;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

[TestFixture]
public class EchoEndpointIntegrationTests
{
    [Test]
    public async Task Should_Return200AndJson_When_GetEchoUnversioned()
    {
        await using var factory = new DiagnosticsWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/echo");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        doc.RootElement.GetProperty("method").GetString().ShouldBe("GET");
        doc.RootElement.GetProperty("scheme").GetString().ShouldNotBeNullOrEmpty();
        doc.RootElement.GetProperty("host").GetString().ShouldNotBeNullOrEmpty();
        doc.RootElement.GetProperty("fullPath").GetString().ShouldBe("/api/echo");
        doc.RootElement.GetProperty("query").GetArrayLength().ShouldBe(0);
    }

    [Test]
    public async Task Should_Return200AndJson_When_GetEchoVersioned()
    {
        await using var factory = new DiagnosticsWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1.0/echo");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        doc.RootElement.GetProperty("method").GetString().ShouldBe("GET");
        doc.RootElement.GetProperty("fullPath").GetString().ShouldBe("/api/v1.0/echo");
    }

    [Test]
    public async Task Should_ReflectQueryString_When_GetEchoWithQuery()
    {
        await using var factory = new DiagnosticsWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/echo?a=1&b=two");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<EchoResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        payload.ShouldNotBeNull();
        payload!.Query.Count.ShouldBe(2);
        payload.Query.ShouldContain(q => q.Key == "a" && q.Value == "1");
        payload.Query.ShouldContain(q => q.Key == "b" && q.Value == "two");
    }

    [Test]
    public async Task Should_RedactSensitiveQueryKeys_When_GetEchoWithSecretInKey()
    {
        await using var factory = new DiagnosticsWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/echo?client_secret=should-not-appear");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.ShouldNotContain("should-not-appear");
        body.ShouldContain("[redacted]");
    }

    [Test]
    public async Task Should_ReflectSafeHeaders_When_CustomHeaderSent()
    {
        await using var factory = new DiagnosticsWebApplicationFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-Test-Echo", "alpha");

        var response = await client.GetAsync("/api/echo");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<EchoResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        payload.ShouldNotBeNull();
        payload!.Headers.TryGetValue("X-Test-Echo", out var v).ShouldBeTrue();
        v.ShouldBe("alpha");
    }

    [Test]
    public async Task Should_NotExposeRawSensitiveHeaders_When_PresentOnRequest()
    {
        await using var factory = new DiagnosticsWebApplicationFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "distinctive-auth-token-xyz");
        client.DefaultRequestHeaders.Add("Cookie", "distinctive-cookie-secret");
        client.DefaultRequestHeaders.Add(ApiKeyConstants.HeaderName, "distinctive-api-key-secret");

        var response = await client.GetAsync("/api/echo");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.ShouldNotContain("distinctive-auth-token-xyz");
        body.ShouldNotContain("distinctive-cookie-secret");
        body.ShouldNotContain("distinctive-api-key-secret");

        var payload = await response.Content.ReadFromJsonAsync<EchoResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        payload.ShouldNotBeNull();
        payload!.SensitiveHeadersPresent.Authorization.ShouldBeTrue();
        payload.SensitiveHeadersPresent.Cookie.ShouldBeTrue();
        payload.SensitiveHeadersPresent.ApiKey.ShouldBeTrue();
    }

    [Test]
    public async Task Should_EnforceApiKey_When_MiddlewareEnabledAndEchoProtected()
    {
        await using var factory = new DiagnosticsApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        (await client.GetAsync("/api/echo")).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await client.GetAsync("/api/v1.0/echo")).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        using var withKey = factory.CreateClient();
        withKey.DefaultRequestHeaders.Add(
            ApiKeyConstants.HeaderName,
            ApiKeyProtectedWebApplicationFactory.TestApiKey);

        (await withKey.GetAsync("/api/echo")).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await withKey.GetAsync("/api/v1.0/echo")).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task Should_Return429_When_RateLimitExceededOnEcho()
    {
        await using var factory = new TunableApiRateLimitWebApplicationFactory(
            new Dictionary<string, string?>
            {
                ["ApiRateLimiting:Enabled"] = "true",
                ["ApiRateLimiting:PermitLimit"] = "2",
                ["ApiRateLimiting:WindowSeconds"] = "2",
                ["ApiRateLimiting:SegmentsPerWindow"] = "2",
                ["ApiRateLimiting:QueueLimit"] = "0",
                ["ApiRateLimiting:ApiKeyHeaderName"] = "X-API-Key"
            });
        using var client = factory.CreateClient();
        (await client.GetAsync("/api/echo")).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await client.GetAsync("/api/echo")).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await client.GetAsync("/api/echo")).StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
    }

    [Test]
    public async Task Should_RejectNonGet_When_PostEcho()
    {
        await using var factory = new DiagnosticsWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsync("/api/echo", null);

        response.StatusCode.ShouldBeOneOf(HttpStatusCode.MethodNotAllowed, HttpStatusCode.NotFound);
    }

    [Test]
    public async Task Should_MapConnectionMetadata_When_GetEcho()
    {
        await using var factory = new DiagnosticsWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/echo");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<EchoResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        payload.ShouldNotBeNull();
        payload!.Connection.ShouldNotBeNull();
    }
}
