using System.Net;
using System.Net.Http.Json;
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
    public async Task Should_Return200AndJsonGreeting_When_GetUnversioned()
    {
        var response = await _client!.GetAsync("/api/hello");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<HelloResponse>();
        payload.ShouldNotBeNull();
        payload!.Message.ShouldBe("Hello, World!");
    }

    [Test]
    public async Task Should_Return200AndJsonGreeting_When_GetVersioned()
    {
        var response = await _client!.GetAsync("/api/v1.0/hello");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<HelloResponse>();
        payload.ShouldNotBeNull();
        payload!.Message.ShouldBe("Hello, World!");
    }

    [Test]
    public async Task Should_Return200WithoutApiKey_When_HelloAndMiddlewareEnabled()
    {
        await using var factory = new ApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var unversioned = await client.GetAsync("/api/hello");
        unversioned.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await unversioned.Content.ReadFromJsonAsync<HelloResponse>())!.Message.ShouldBe("Hello, World!");

        var versioned = await client.GetAsync("/api/v1.0/hello");
        versioned.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await versioned.Content.ReadFromJsonAsync<HelloResponse>())!.Message.ShouldBe("Hello, World!");
    }
}
