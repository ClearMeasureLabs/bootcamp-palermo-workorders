using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using ClearMeasure.Bootcamp.UnitTests.UI.Server;
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
    public async Task Should_Return200WithRandomNumber_When_GetUnversioned()
    {
        var response = await _client!.GetAsync("/api/tools/random?type=number");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("text/plain");
        var body = await response.Content.ReadAsStringAsync();
        int.TryParse(body, NumberStyles.Integer, CultureInfo.InvariantCulture, out _).ShouldBeTrue();
    }

    [Test]
    public async Task Should_Return200WithRandomNumber_When_GetVersioned()
    {
        var response = await _client!.GetAsync("/api/v1.0/tools/random?type=number");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("text/plain");
        var body = await response.Content.ReadAsStringAsync();
        int.TryParse(body, NumberStyles.Integer, CultureInfo.InvariantCulture, out _).ShouldBeTrue();
    }

    [Test]
    public async Task Should_Return200WithRandomString_When_GetString()
    {
        var response = await _client!.GetAsync("/api/tools/random?type=string");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Length.ShouldBe(16);
        Regex.IsMatch(body, "^[a-zA-Z0-9]+$").ShouldBeTrue();
    }

    [Test]
    public async Task Should_Return200WithUuid_When_GetUuid()
    {
        var response = await _client!.GetAsync("/api/tools/random?type=uuid");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        Guid.TryParse(body, out _).ShouldBeTrue();
    }

    [Test]
    public async Task Should_Return200WithColor_When_GetColor()
    {
        var response = await _client!.GetAsync("/api/tools/random?type=color");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        Regex.IsMatch(body, "^#[0-9A-Fa-f]{6}$").ShouldBeTrue();
    }

    [Test]
    public async Task Should_Return200AndVary_When_CalledTwiceForNumber()
    {
        var first = await _client!.GetAsync("/api/tools/random?type=number&min=0&max=1000000");
        var second = await _client.GetAsync("/api/tools/random?type=number&min=0&max=1000000");

        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        second.StatusCode.ShouldBe(HttpStatusCode.OK);
        var firstBody = await first.Content.ReadAsStringAsync();
        var secondBody = await second.Content.ReadAsStringAsync();
        firstBody.ShouldNotBe(secondBody);
    }

    [Test]
    public async Task Should_Return400_When_TypeMissing()
    {
        var response = await _client!.GetAsync("/api/tools/random");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("problem+json");
    }

    [Test]
    public async Task Should_Return400_When_InvalidType()
    {
        var response = await _client!.GetAsync("/api/tools/random?type=invalid");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("problem+json");
    }

    [Test]
    public async Task Should_Return400_When_MinGreaterThanMax()
    {
        var response = await _client!.GetAsync("/api/tools/random?type=number&min=100&max=50");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("problem+json");
    }

    [Test]
    public async Task Should_Return200WithoutApiKey_When_MiddlewareEnabled()
    {
        await using var factory = new ApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var unversioned = await client.GetAsync("/api/tools/random?type=number");
        unversioned.StatusCode.ShouldBe(HttpStatusCode.OK);

        var versioned = await client.GetAsync("/api/v1.0/tools/random?type=number");
        versioned.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
