using System.Net;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UnitTests.UI.Server;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

[TestFixture]
public class QuoteEndpointIntegrationTests
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
    public async Task Should_Return200AndPlainTextQuote_When_GetUnversioned()
    {
        var response = await _client!.GetAsync("/api/quote");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("text/plain");
        (await response.Content.ReadAsStringAsync()).ShouldBe(QuoteConstants.DefaultText);
    }

    [Test]
    public async Task Should_Return200AndPlainTextQuote_When_GetVersioned()
    {
        var response = await _client!.GetAsync("/api/v1.0/quote");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("text/plain");
        (await response.Content.ReadAsStringAsync()).ShouldBe(QuoteConstants.DefaultText);
    }

    [Test]
    public async Task Should_Return200WithoutApiKey_When_QuoteAndMiddlewareEnabled()
    {
        await using var factory = new ApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var unversioned = await client.GetAsync("/api/quote");
        unversioned.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await unversioned.Content.ReadAsStringAsync()).ShouldBe(QuoteConstants.DefaultText);

        var versioned = await client.GetAsync("/api/v1.0/quote");
        versioned.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await versioned.Content.ReadAsStringAsync()).ShouldBe(QuoteConstants.DefaultText);
    }
}
