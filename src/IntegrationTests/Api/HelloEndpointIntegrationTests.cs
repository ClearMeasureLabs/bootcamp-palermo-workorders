using System.Net;
using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UnitTests.UI.Server;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

[TestFixture]
public class HelloEndpointIntegrationTests
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
    public async Task Should_Return200AndJsonHelloMessage_When_GetUnversioned()
    {
        var response = await _client!.GetAsync("/api/hello");

        await AssertHelloResponse(response);
    }

    [Test]
    public async Task Should_Return200AndJsonHelloMessage_When_GetVersioned()
    {
        var response = await _client!.GetAsync("/api/v1.0/hello");

        await AssertHelloResponse(response);
    }

    [Test]
    public async Task Should_Return200WithoutApiKey_When_HelloAndMiddlewareEnabled()
    {
        await using var factory = new ApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        await AssertHelloResponse(await client.GetAsync("/api/hello"));
        await AssertHelloResponse(await client.GetAsync("/api/v1.0/hello"));
    }

    private static async Task AssertHelloResponse(HttpResponseMessage response)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");

        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        document.RootElement.GetProperty("message").GetString().ShouldBe("Hello, World!");
    }
}
