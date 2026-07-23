using System.Net;
using ClearMeasure.Bootcamp.UnitTests.UI.Server;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

[TestFixture]
public class CoinFlipEndpointIntegrationTests
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
    public async Task Should_Return200AndPlainTextHeadsOrTails_When_GetUnversioned()
    {
        var response = await _client!.GetAsync("/api/coinflip");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("text/plain");
        (await response.Content.ReadAsStringAsync()).ShouldBeOneOf("heads", "tails");
    }

    [Test]
    public async Task Should_Return200AndPlainTextHeadsOrTails_When_GetVersioned()
    {
        var response = await _client!.GetAsync("/api/v1.0/coinflip");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("text/plain");
        (await response.Content.ReadAsStringAsync()).ShouldBeOneOf("heads", "tails");
    }

    [Test]
    public async Task Should_Return200WithoutApiKey_When_CoinFlipAndMiddlewareEnabled()
    {
        await using var factory = new ApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var unversioned = await client.GetAsync("/api/coinflip");
        unversioned.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await unversioned.Content.ReadAsStringAsync()).ShouldBeOneOf("heads", "tails");

        var versioned = await client.GetAsync("/api/v1.0/coinflip");
        versioned.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await versioned.Content.ReadAsStringAsync()).ShouldBeOneOf("heads", "tails");
    }
}
