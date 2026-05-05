using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

[TestFixture]
public class ToolsRandomEndpointIntegrationTests
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
    public async Task Should_Return200_ForEachSupportedType_OnUnversionedRoute()
    {
        await AssertRandomResponseAsync("number", el =>
        {
            el.GetProperty("type").GetString().ShouldBe("number");
            var n = el.GetProperty("value").GetInt32();
            n.ShouldBeGreaterThanOrEqualTo(0);
            n.ShouldBeLessThanOrEqualTo(int.MaxValue - 1);
        });

        await AssertRandomResponseAsync("string", el =>
        {
            el.GetProperty("type").GetString().ShouldBe("string");
            var s = el.GetProperty("value").GetString();
            s.ShouldNotBeNull();
            s!.Length.ShouldBe(16);
            Regex.IsMatch(s, @"^[A-Za-z0-9]{16}$").ShouldBeTrue();
        });

        await AssertRandomResponseAsync("uuid", el =>
        {
            el.GetProperty("type").GetString().ShouldBe("uuid");
            var s = el.GetProperty("value").GetString();
            Guid.TryParse(s, out _).ShouldBeTrue();
        });

        await AssertRandomResponseAsync("color", el =>
        {
            el.GetProperty("type").GetString().ShouldBe("color");
            var s = el.GetProperty("value").GetString();
            Regex.IsMatch(s!, "^#[0-9A-F]{6}$").ShouldBeTrue();
        });
    }

    [Test]
    public async Task Should_AcceptTypeCaseInsensitively_OnUnversionedRoute()
    {
        var response = await _client!.GetAsync("/api/tools/random?type=NUMBER");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task Should_Return200_ForSupportedTypes_OnVersionedV1Route()
    {
        var numberResponse = await _client!.GetAsync("/api/v1.0/tools/random?type=number");
        numberResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var uuidResponse = await _client.GetAsync("/api/v1.0/tools/random?type=uuid");
        uuidResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task Should_Return400_WithProblemDetails_When_TypeInvalid()
    {
        var missing = await _client!.GetAsync("/api/tools/random");
        missing.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var empty = await _client.GetAsync("/api/tools/random?type=");
        empty.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var unknown = await _client.GetAsync("/api/tools/random?type=nope");
        unknown.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var mediaType = unknown.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("problem");
    }

    [Test]
    public async Task Should_NotUseConditionalGetCaching_When_RepeatedGets()
    {
        var first = await _client!.GetAsync("/api/tools/random?type=uuid");
        var second = await _client.GetAsync("/api/tools/random?type=uuid");
        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        second.StatusCode.ShouldBe(HttpStatusCode.OK);
        first.Headers.Contains("ETag").ShouldBeFalse();
        second.Headers.Contains("ETag").ShouldBeFalse();
    }

    [Test]
    public async Task Should_AllowAnonymous_When_ApiKeyMiddlewareEnabled()
    {
        await using var factory = new DiagnosticsApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/tools/random?type=number");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var versioned = await client.GetAsync("/api/v1.0/tools/random?type=uuid");
        versioned.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    private async Task AssertRandomResponseAsync(string type, Action<JsonElement> assert)
    {
        var response = await _client!.GetAsync($"/api/tools/random?type={type}");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");
        response.Headers.Contains("ETag").ShouldBeFalse();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        assert(doc.RootElement);
    }
}
