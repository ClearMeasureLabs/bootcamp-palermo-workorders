using System.Net;
using System.Text.RegularExpressions;
using ClearMeasure.Bootcamp.UnitTests.UI.Server;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

[TestFixture]
public class ColorEndpointIntegrationTests
{
    private static readonly Regex HexColorPattern = new(@"^#[0-9A-F]{6}$", RegexOptions.CultureInvariant);

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
    public async Task Should_Return200AndPlainTextHexColor_When_GetUnversioned()
    {
        var response = await _client!.GetAsync("/api/color");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("text/plain");
        var body = await response.Content.ReadAsStringAsync();
        HexColorPattern.IsMatch(body).ShouldBeTrue();
    }

    [Test]
    public async Task Should_Return200AndPlainTextHexColor_When_GetVersioned()
    {
        var response = await _client!.GetAsync("/api/v1.0/color");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("text/plain");
        var body = await response.Content.ReadAsStringAsync();
        HexColorPattern.IsMatch(body).ShouldBeTrue();
    }

    [Test]
    public async Task Should_Return200WithoutApiKey_When_ColorAndMiddlewareEnabled()
    {
        await using var factory = new ApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var unversioned = await client.GetAsync("/api/color");
        unversioned.StatusCode.ShouldBe(HttpStatusCode.OK);
        HexColorPattern.IsMatch(await unversioned.Content.ReadAsStringAsync()).ShouldBeTrue();

        var versioned = await client.GetAsync("/api/v1.0/color");
        versioned.StatusCode.ShouldBe(HttpStatusCode.OK);
        HexColorPattern.IsMatch(await versioned.Content.ReadAsStringAsync()).ShouldBeTrue();
    }
}
