using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using ClearMeasure.Bootcamp.UI.Server.RateLimiting;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UnitTests.Api;
using ClearMeasure.Bootcamp.UnitTests.UI.Server;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

[TestFixture]
public class ToolsRandomEndpointIntegrationTests
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
    public async Task Should_Return200AndJson_When_TypeIsNumber()
    {
        var response = await _client!.GetAsync("/api/tools/random?type=number");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        doc.RootElement.GetProperty("type").GetString().ShouldBe("number");
        doc.RootElement.GetProperty("value").GetInt32().ShouldBeInRange(int.MinValue, int.MaxValue);
    }

    [Test]
    public async Task Should_Return200AndJson_When_TypeIsString()
    {
        var response = await _client!.GetAsync("/api/tools/random?type=string");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        doc.RootElement.GetProperty("type").GetString().ShouldBe("string");
        var s = doc.RootElement.GetProperty("value").GetString();
        s.ShouldNotBeNullOrEmpty();
        s!.Length.ShouldBe(24);
        Regex.IsMatch(s, "^[a-zA-Z0-9]{24}$").ShouldBeTrue();
    }

    [Test]
    public async Task Should_Return200AndJson_When_TypeIsUuid()
    {
        var response = await _client!.GetAsync("/api/tools/random?type=uuid");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        doc.RootElement.GetProperty("type").GetString().ShouldBe("uuid");
        var s = doc.RootElement.GetProperty("value").GetString();
        Guid.TryParse(s, out var guid).ShouldBeTrue();
        guid.ToString("D").ShouldBe(s);
    }

    [Test]
    public async Task Should_Return200AndJson_When_TypeIsColor()
    {
        var response = await _client!.GetAsync("/api/tools/random?type=color");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        doc.RootElement.GetProperty("type").GetString().ShouldBe("color");
        var s = doc.RootElement.GetProperty("value").GetString();
        Regex.IsMatch(s!, "^#[0-9a-f]{6}$").ShouldBeTrue();
    }

    [Test]
    public async Task Should_Return400ProblemDetails_When_TypeIsMissing()
    {
        var response = await _client!.GetAsync("/api/tools/random");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/problem+json");

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("status").GetInt32().ShouldBe(400);
        doc.RootElement.GetProperty("detail").GetString()!.ShouldContain("type");
    }

    [Test]
    public async Task Should_Return400ProblemDetails_When_TypeIsWhitespaceOnly()
    {
        var response = await _client!.GetAsync("/api/tools/random?type=%20%20");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/problem+json");
    }

    [Test]
    public async Task Should_Return400ProblemDetails_When_TypeIsUnknown()
    {
        var response = await _client!.GetAsync("/api/tools/random?type=not-a-real-type");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/problem+json");

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("status").GetInt32().ShouldBe(400);
        doc.RootElement.GetProperty("detail").GetString()!.ShouldContain("Unsupported");
    }

    [Test]
    public async Task Should_ExposeSameBehavior_When_VersionedPathUsed()
    {
        var a = await _client!.GetAsync("/api/tools/random?type=uuid");
        var b = await _client.GetAsync("/api/v1.0/tools/random?type=uuid");

        a.StatusCode.ShouldBe(HttpStatusCode.OK);
        b.StatusCode.ShouldBe(HttpStatusCode.OK);

        var ja = JsonDocument.Parse(await a.Content.ReadAsStringAsync());
        var jb = JsonDocument.Parse(await b.Content.ReadAsStringAsync());
        ja.RootElement.GetProperty("type").GetString().ShouldBe(jb.RootElement.GetProperty("type").GetString());
        ja.RootElement.GetProperty("value").GetString()!.Length.ShouldBe(36);
        jb.RootElement.GetProperty("value").GetString()!.Length.ShouldBe(36);
    }

    [Test]
    public async Task Should_AllowAnonymousAccess_When_ApiKeyDisabled()
    {
        var response = await _client!.GetAsync("/api/tools/random?type=number");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task Should_Return401_When_ApiKeyRequiredAndHeaderMissing()
    {
        await using var factory = new ApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/health");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Should_Return200_When_ApiKeyRequiredAndToolsRandomWithoutHeader()
    {
        await using var factory = new ApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/tools/random?type=uuid");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task Should_Return200_When_ApiKeyRequiredAndValidHeaderPresent()
    {
        await using var factory = new ApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(ApiKeyConstants.HeaderName, ApiKeyProtectedWebApplicationFactory.TestApiKey);

        var response = await client.GetAsync("/api/tools/random?type=uuid");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task Should_IncludeRateLimitHeaders_When_RateLimitingEnabled()
    {
        var settings = new Dictionary<string, string?>
        {
            ["ApiRateLimiting:Enabled"] = "true",
            ["ApiRateLimiting:PermitLimit"] = "10",
            ["ApiRateLimiting:WindowSeconds"] = "2",
            ["ApiRateLimiting:SegmentsPerWindow"] = "2",
            ["ApiRateLimiting:QueueLimit"] = "0",
            ["ApiRateLimiting:ApiKeyHeaderName"] = "X-API-Key"
        };

        await using var factory = new TunableApiRateLimitWebApplicationFactory(settings);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/tools/random?type=number");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.TryGetValues(RateLimitingMiddleware.HeaderLimit, out var limit).ShouldBeTrue();
        response.Headers.TryGetValues(RateLimitingMiddleware.HeaderRemaining, out var remaining).ShouldBeTrue();
        limit!.First().ShouldBe("10");
        int.Parse(remaining!.First(), NumberFormatInfo.InvariantInfo).ShouldBeGreaterThanOrEqualTo(0);
    }
}
