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
    public async Task Should_Return200AndJson_When_GetVersionUnversioned()
    {
        var response = await _client!.GetAsync("/api/version");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = doc.RootElement;
        root.TryGetProperty("assemblyVersion", out _).ShouldBeTrue();
        root.TryGetProperty("informationalVersion", out _).ShouldBeTrue();
        root.TryGetProperty("buildConfiguration", out _).ShouldBeTrue();
        root.TryGetProperty("environment", out _).ShouldBeTrue();
        root.TryGetProperty("machineName", out _).ShouldBeTrue();
        root.TryGetProperty("frameworkDescription", out _).ShouldBeTrue();
    }

    [Test]
    public async Task Should_Return200AndJson_When_GetVersionVersioned()
    {
        var response = await _client!.GetAsync("/api/v1.0/version");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = doc.RootElement;
        root.TryGetProperty("assemblyVersion", out _).ShouldBeTrue();
        root.TryGetProperty("informationalVersion", out _).ShouldBeTrue();
        root.TryGetProperty("buildConfiguration", out _).ShouldBeTrue();
        root.TryGetProperty("environment", out _).ShouldBeTrue();
        root.TryGetProperty("machineName", out _).ShouldBeTrue();
        root.TryGetProperty("frameworkDescription", out _).ShouldBeTrue();
    }

    [Test]
    public async Task Should_Return200WithoutApiKey_When_VersionAndMiddlewareEnabled()
    {
        await using var factory = new ApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var unversioned = await client.GetAsync("/api/version");
        unversioned.StatusCode.ShouldBe(HttpStatusCode.OK);

        var versioned = await client.GetAsync("/api/v1.0/version");
        versioned.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
