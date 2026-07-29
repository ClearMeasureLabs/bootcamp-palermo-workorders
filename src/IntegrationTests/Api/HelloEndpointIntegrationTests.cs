using System.Net;
using System.Text.Json;
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
    public async Task Should_Return200AndJsonHelloWorld_When_GetUnversioned()
    {
        var response = await _client!.GetAsync("/api/hello");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");
        
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        doc.RootElement.TryGetProperty("message", out var messageProperty).ShouldBeTrue();
        messageProperty.GetString().ShouldBe("Hello, World!");
    }

    [Test]
    public async Task Should_Return200AndJsonHelloWorld_When_GetVersioned()
    {
        var response = await _client!.GetAsync("/api/v1.0/hello");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");
        
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        doc.RootElement.TryGetProperty("message", out var messageProperty).ShouldBeTrue();
        messageProperty.GetString().ShouldBe("Hello, World!");
    }

    [Test]
    public async Task Should_Return200WithoutApiKey_When_HelloAndMiddlewareEnabled()
    {
        await using var factory = new ApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var unversioned = await client.GetAsync("/api/hello");
        unversioned.StatusCode.ShouldBe(HttpStatusCode.OK);
        var json1 = await unversioned.Content.ReadAsStringAsync();
        var doc1 = JsonDocument.Parse(json1);
        doc1.RootElement.TryGetProperty("message", out var msg1).ShouldBeTrue();
        msg1.GetString().ShouldBe("Hello, World!");

        var versioned = await client.GetAsync("/api/v1.0/hello");
        versioned.StatusCode.ShouldBe(HttpStatusCode.OK);
        var json2 = await versioned.Content.ReadAsStringAsync();
        var doc2 = JsonDocument.Parse(json2);
        doc2.RootElement.TryGetProperty("message", out var msg2).ShouldBeTrue();
        msg2.GetString().ShouldBe("Hello, World!");
    }
}
