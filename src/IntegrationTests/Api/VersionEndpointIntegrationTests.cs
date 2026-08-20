using System.Net;
using System.Text.Json;
using ClearMeasure.Bootcamp.UnitTests.UI.Server;
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
    public async Task Should_Return200AndJsonMetadata_When_GetUnversioned()
    {
        var response = await _client!.GetAsync("/api/version");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        await AssertVersionJsonShape(response);
    }

    [Test]
    public async Task Should_Return200AndJsonMetadata_When_GetVersioned()
    {
        var response = await _client!.GetAsync("/api/v1.0/version");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        await AssertVersionJsonShape(response);
    }

    [Test]
    public async Task Should_Return200WithoutApiKey_When_VersionAndMiddlewareEnabled()
    {
        await using var factory = new ApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var unversioned = await client.GetAsync("/api/version");
        unversioned.StatusCode.ShouldBe(HttpStatusCode.OK);
        await AssertVersionJsonShape(unversioned);

        var versioned = await client.GetAsync("/api/v1.0/version");
        versioned.StatusCode.ShouldBe(HttpStatusCode.OK);
        await AssertVersionJsonShape(versioned);
    }

    [Test]
    public async Task Should_HaveCorrectContentType()
    {
        var response = await _client!.GetAsync("/api/version");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");
        response.Content.Headers.ContentType!.CharSet.ShouldNotBeNullOrEmpty();
    }

    private static async Task AssertVersionJsonShape(HttpResponseMessage response)
    {
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = doc.RootElement;
        root.GetProperty("assemblyVersion").GetString().ShouldNotBeNullOrEmpty();
        root.GetProperty("informationalVersion").GetString().ShouldNotBeNullOrEmpty();
        root.GetProperty("buildConfiguration").GetString().ShouldNotBeNullOrEmpty();
        root.GetProperty("environment").GetString().ShouldNotBeNullOrEmpty();
    }
}
