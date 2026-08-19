using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using ClearMeasure.Bootcamp.UI.Server.RateLimiting;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UnitTests.Api;
using ClearMeasure.Bootcamp.UnitTests.UI.Server;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

[TestFixture]
public class RandomEndpointIntegrationTests
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
    public async Task Should_Return200AndPlainText_When_GetRandomNumber()
    {
        var response = await _client!.GetAsync("/api/tools/random?type=number");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("text/plain");
        int.TryParse(await response.Content.ReadAsStringAsync(), NumberStyles.Integer, CultureInfo.InvariantCulture, out _).ShouldBeTrue();
    }

    [Test]
    public async Task Should_Return200AndPlainText_When_GetRandomString()
    {
        var response = await _client!.GetAsync("/api/tools/random?type=string&length=15");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Length.ShouldBe(15);
    }

    [Test]
    public async Task Should_Return200AndPlainText_When_GetRandomUuid()
    {
        var response = await _client!.GetAsync("/api/tools/random?type=uuid");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        Guid.TryParse(await response.Content.ReadAsStringAsync(), out _).ShouldBeTrue();
    }

    [Test]
    public async Task Should_Return200AndPlainText_When_GetRandomColor()
    {
        var response = await _client!.GetAsync("/api/tools/random?type=color");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        Regex.IsMatch(body, "^#[0-9A-F]{6}$").ShouldBeTrue();
    }

    [Test]
    public async Task Should_Return200AndPlainText_When_GetVersionedRoute()
    {
        var response = await _client!.GetAsync("/api/v1.0/tools/random?type=number");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("text/plain");
    }

    [Test]
    public async Task Should_Return400_When_TypeMissing()
    {
        var response = await _client!.GetAsync("/api/tools/random");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.ShouldContain("type");
    }

    [Test]
    public async Task Should_Return400_When_TypeInvalid()
    {
        var response = await _client!.GetAsync("/api/tools/random?type=not-a-type");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.ShouldContain("Invalid type");
    }

    [Test]
    public async Task Should_BypassApiKey_When_MiddlewareEnabledAndEndpointPublic()
    {
        await using var factory = new ApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var withoutKey = await client.GetAsync("/api/tools/random?type=uuid");
        withoutKey.StatusCode.ShouldBe(HttpStatusCode.OK);
        Guid.TryParse(await withoutKey.Content.ReadAsStringAsync(), out _).ShouldBeTrue();

        using var withKeyRequest = new HttpRequestMessage(HttpMethod.Get, "/api/tools/random?type=uuid");
        withKeyRequest.Headers.Add(
            ApiKeyConstants.HeaderName,
            ApiKeyProtectedWebApplicationFactory.TestApiKey);
        var withKey = await client.SendAsync(withKeyRequest);
        withKey.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task Should_EnforceRateLimiting_When_EndpointHasPolicy()
    {
        await using var factory = new RateLimitedApiWebApplicationFactory();
        using var client = factory.CreateClient();

        (await client.GetAsync("/api/tools/random?type=number")).StatusCode.ShouldBe(HttpStatusCode.OK);

        var limited = await client.GetAsync("/api/tools/random?type=number");
        limited.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        limited.Headers.TryGetValues(RateLimitingMiddleware.HeaderLimit, out _).ShouldBeTrue();
    }
}
