using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using ClearMeasure.Bootcamp.UI.Shared;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class EchoEndpointTests
{
    [Test]
    public async Task Should_Return200AndJsonWithCoreFields_When_GetEcho_LegacyAndV1Paths()
    {
        await using var factory = new ApiVersioningRoutingWebApplicationFactory();
        using var client = factory.CreateClient();

        var legacy = await client.GetAsync("/api/echo");
        var v1 = await client.GetAsync("/api/v1.0/echo");

        legacy.StatusCode.ShouldBe(HttpStatusCode.OK);
        v1.StatusCode.ShouldBe(HttpStatusCode.OK);
        (legacy.Content.Headers.ContentType?.MediaType ?? "").ShouldContain("application/json");
        (v1.Content.Headers.ContentType?.MediaType ?? "").ShouldContain("application/json");

        var legacyBody = await legacy.Content.ReadFromJsonAsync<EchoResponseStub>();
        var v1Body = await v1.Content.ReadFromJsonAsync<EchoResponseStub>();
        legacyBody.ShouldNotBeNull();
        v1Body.ShouldNotBeNull();
        legacyBody!.Method.ShouldBe("GET");
        v1Body!.Method.ShouldBe("GET");
        legacyBody.Path.ShouldBe("/api/echo");
        v1Body.Path.ShouldBe("/api/v1.0/echo");
    }

    [Test]
    public async Task Should_ReflectQueryString_When_GetEchoWithQuery()
    {
        await using var factory = new ApiVersioningRoutingWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/echo?a=1&b=two");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<EchoResponseStub>();
        body.ShouldNotBeNull();
        body!.QueryString.ShouldBe("?a=1&b=two");
        body.Url.ShouldContain("a=1");
        body.Url.ShouldContain("b=two");
    }

    [Test]
    public async Task Should_NotLeakSecretHeaders_When_SensitiveHeadersPresent()
    {
        await using var factory = new ApiVersioningRoutingWebApplicationFactory();
        using var client = factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/echo");
        request.Headers.TryAddWithoutValidation("Authorization", "Bearer super-secret-token-12345");
        request.Headers.TryAddWithoutValidation("Cookie", "session=evil");
        request.Headers.TryAddWithoutValidation("X-Api-Key", "should-not-appear");

        var response = await client.SendAsync(request);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var raw = await response.Content.ReadAsStringAsync();
        raw.ShouldNotContain("super-secret-token-12345");
        raw.ShouldNotContain("evil");
        raw.ShouldNotContain("should-not-appear");

        using var doc = JsonDocument.Parse(raw);
        foreach (var h in doc.RootElement.GetProperty("headers").EnumerateArray())
        {
            var name = h.GetProperty("name").GetString();
            var value = h.GetProperty("value").GetString();
            if (string.Equals(name, "Authorization", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "Cookie", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "X-Api-Key", StringComparison.OrdinalIgnoreCase))
            {
                value.ShouldBe(EchoResponse.RedactedPlaceholder);
            }
        }
    }

    [Test]
    public async Task Should_IncludeCustomHeader_When_NonSensitiveHeaderSent()
    {
        await using var factory = new ApiVersioningRoutingWebApplicationFactory();
        using var client = factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/echo");
        request.Headers.TryAddWithoutValidation("X-Test-Echo", "diagnostic-value");

        var response = await client.SendAsync(request);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<EchoResponseStub>();
        body.ShouldNotBeNull();
        body!.Headers.ShouldContain(h =>
            string.Equals(h.Name, "X-Test-Echo", StringComparison.OrdinalIgnoreCase)
            && h.Value == "diagnostic-value");
    }

    [Test]
    public async Task Should_Return200WithoutApiKey_When_MiddlewareEnabled_BothEchoPaths()
    {
        await using var factory = new ApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var legacy = await client.GetAsync("/api/echo");
        var v1 = await client.GetAsync("/api/v1.0/echo");

        legacy.StatusCode.ShouldBe(HttpStatusCode.OK);
        v1.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task Should_ReturnNotSuccess_When_GetEcho_UnsupportedVersion()
    {
        await using var factory = new ApiVersioningRoutingWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v2.0/echo");

        response.IsSuccessStatusCode.ShouldBeFalse();
        response.StatusCode.ShouldBeOneOf(HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Should_NotReturnSuccessfulEcho_When_PostToEcho()
    {
        await using var factory = new ApiVersioningRoutingWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsync("/api/echo", null);

        response.IsSuccessStatusCode.ShouldBeFalse();
        response.StatusCode.ShouldBeOneOf(HttpStatusCode.MethodNotAllowed, HttpStatusCode.NotFound);
    }

    private sealed class EchoResponseStub
    {
        public string Method { get; set; } = "";
        public string Path { get; set; } = "";
        public string QueryString { get; set; } = "";
        public string Url { get; set; } = "";
        public List<EchoHeaderEntryStub> Headers { get; set; } = [];
    }

    private sealed class EchoHeaderEntryStub
    {
        public string Name { get; set; } = "";
        public string Value { get; set; } = "";
    }
}
