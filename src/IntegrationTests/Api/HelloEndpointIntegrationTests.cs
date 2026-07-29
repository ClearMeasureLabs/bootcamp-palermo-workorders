using System.Net;
using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
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
    public async Task Should_ReturnHttp200_When_GetUnversioned()
    {
        var response = await _client!.GetAsync("/api/hello");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task Should_ReturnExpectedJson_When_GetUnversioned()
    {
        var response = await _client!.GetAsync("/api/hello");

        var payload = JsonSerializer.Deserialize<HelloResponse>(
            await response.Content.ReadAsStringAsync(),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        payload.ShouldNotBeNull();
        payload!.Message.ShouldBe("Hello, World!");
    }

    [Test]
    public async Task Should_ReturnHttp200_When_GetVersioned()
    {
        var response = await _client!.GetAsync("/api/v1.0/hello");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task Should_ReturnExpectedJson_When_GetVersioned()
    {
        var response = await _client!.GetAsync("/api/v1.0/hello");

        var payload = JsonSerializer.Deserialize<HelloResponse>(
            await response.Content.ReadAsStringAsync(),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        payload.ShouldNotBeNull();
        payload!.Message.ShouldBe("Hello, World!");
    }

    [Test]
    public async Task Should_AllowAnonymousAccess_When_ApiKeyMiddlewareEnabled()
    {
        await using var factory = new ApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var unversioned = await client.GetAsync("/api/hello");
        unversioned.StatusCode.ShouldBe(HttpStatusCode.OK);

        var versioned = await client.GetAsync("/api/v1.0/hello");
        versioned.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
