using System.Net;
using System.Text.RegularExpressions;
using ClearMeasure.Bootcamp.UnitTests.UI.Server;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

[TestFixture]
public class ToolsRandomEndpointIntegrationTests
{
    private static readonly Regex HexColorRegex = new("^#[0-9A-F]{6}$", RegexOptions.CultureInvariant);
    private static readonly Regex AlphanumericRegex = new("^[a-zA-Z0-9]+$", RegexOptions.CultureInvariant);

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

    [TestCase("number")]
    [TestCase("string")]
    [TestCase("uuid")]
    [TestCase("color")]
    public async Task Should_Return200AndValidPayload_When_GetUnversioned_ForEachType(string type)
    {
        var response = await _client!.GetAsync($"/api/tools/random?type={type}");

        await AssertValidPayloadAsync(response, type);
    }

    [TestCase("number")]
    [TestCase("string")]
    [TestCase("uuid")]
    [TestCase("color")]
    public async Task Should_Return200AndValidPayload_When_GetVersioned_ForEachType(string type)
    {
        var response = await _client!.GetAsync($"/api/v1.0/tools/random?type={type}");

        await AssertValidPayloadAsync(response, type);
    }

    [TestCase("/api/tools/random")]
    [TestCase("/api/tools/random?type=")]
    [TestCase("/api/tools/random?type=banana")]
    [TestCase("/api/v1.0/tools/random")]
    [TestCase("/api/v1.0/tools/random?type=unknown")]
    public async Task Should_Return400_When_TypeMissingOrUnknown(string path)
    {
        var response = await _client!.GetAsync(path);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Should_Return200WithoutApiKey_When_MiddlewareEnabled()
    {
        await using var factory = new ApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var unversioned = await client.GetAsync("/api/tools/random?type=uuid");
        await AssertValidPayloadAsync(unversioned, "uuid");

        var versioned = await client.GetAsync("/api/v1.0/tools/random?type=color");
        await AssertValidPayloadAsync(versioned, "color");
    }

    private static async Task AssertValidPayloadAsync(HttpResponseMessage response, string type)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType.ShouldContain("text/plain");
        var body = await response.Content.ReadAsStringAsync();
        body.ShouldNotBeNullOrWhiteSpace();

        switch (type.ToLowerInvariant())
        {
            case "number":
                int.TryParse(body, out _).ShouldBeTrue();
                break;
            case "string":
                AlphanumericRegex.IsMatch(body).ShouldBeTrue();
                break;
            case "uuid":
                Guid.TryParseExact(body, "D", out _).ShouldBeTrue();
                break;
            case "color":
                HexColorRegex.IsMatch(body).ShouldBeTrue();
                break;
            default:
                Assert.Fail($"Unexpected type under test: {type}");
                break;
        }
    }
}
