using System.Net;
using System.Text.Json;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class HelloEndpointTests
{
    private ApiVersioningRoutingWebApplicationFactory? _factory;
    private HttpClient? _client;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _factory = new ApiVersioningRoutingWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    [Test]
    public async Task Should_ReturnJsonResult_When_HelloEndpointInvoked()
    {
        var response = await _client!.GetAsync("/api/hello");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");
        
        var content = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(content);
        json.RootElement.GetProperty("message").GetString().ShouldBe("Hello, World!");
    }

    [Test]
    public async Task Should_Return200AndSamePayload_When_GetHello_LegacyAndV1Paths()
    {
        var legacy = await _client!.GetAsync("/api/hello");
        var v1 = await _client.GetAsync("/api/v1.0/hello");

        legacy.StatusCode.ShouldBe(HttpStatusCode.OK);
        v1.StatusCode.ShouldBe(HttpStatusCode.OK);

        using var legacyDoc = JsonDocument.Parse(await legacy.Content.ReadAsStringAsync());
        using var v1Doc = JsonDocument.Parse(await v1.Content.ReadAsStringAsync());
        legacyDoc.RootElement.GetProperty("message").GetString().ShouldBe(v1Doc.RootElement.GetProperty("message").GetString());
        legacyDoc.RootElement.GetProperty("message").GetString().ShouldBe("Hello, World!");
    }
}
