using System.Net;
using System.Text.Json;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

[TestFixture]
public class VersionEndpointIntegrationTests
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
    public async Task Should_Return200_When_GetVersionEndpoint()
    {
        var response = await _client!.GetAsync("/_version");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task Should_ReturnApplicationJson_When_GetVersionEndpoint()
    {
        var response = await _client!.GetAsync("/_version");

        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType.ShouldContain("application/json");
    }

    [Test]
    public async Task Should_ReturnJsonBodyWithVersionProperty_When_GetVersionEndpoint()
    {
        var response = await _client!.GetAsync("/_version");

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        doc.RootElement.TryGetProperty("version", out var versionProp).ShouldBeTrue();
        versionProp.GetString().ShouldNotBeNullOrEmpty();
    }
}
