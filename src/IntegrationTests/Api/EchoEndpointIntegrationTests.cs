using System.Net;
using System.Text.Json;
using ClearMeasure.Bootcamp.UnitTests.UI.Server;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

[TestFixture]
public class EchoEndpointIntegrationTests
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
    public async Task Should_Return200AndJson_When_GetUnversioned()
    {
        var response = await _client!.GetAsync("/api/echo?probe=1");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        doc.RootElement.TryGetProperty("method", out var method).ShouldBeTrue();
        method.GetString().ShouldBe("GET");
        doc.RootElement.TryGetProperty("path", out var path).ShouldBeTrue();
        path.GetString().ShouldBe("/api/echo");
        doc.RootElement.TryGetProperty("queryString", out var queryString).ShouldBeTrue();
        queryString.GetString().ShouldContain("probe=1");
        doc.RootElement.TryGetProperty("scheme", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("host", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("protocol", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("headers", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("query", out var query).ShouldBeTrue();
        query.TryGetProperty("probe", out var probe).ShouldBeTrue();
        probe.GetString().ShouldBe("1");
    }

    [Test]
    public async Task Should_Return200AndJson_When_GetVersioned()
    {
        var response = await _client!.GetAsync("/api/v1.0/echo");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        doc.RootElement.TryGetProperty("method", out var method).ShouldBeTrue();
        method.GetString().ShouldBe("GET");
        doc.RootElement.TryGetProperty("path", out var path).ShouldBeTrue();
        path.GetString().ShouldBe("/api/v1.0/echo");
        doc.RootElement.TryGetProperty("headers", out _).ShouldBeTrue();
    }

    [Test]
    public async Task Should_Return200WithoutApiKey_When_EchoAndMiddlewareEnabled()
    {
        await using var factory = new ApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var unversioned = await client.GetAsync("/api/echo");
        unversioned.StatusCode.ShouldBe(HttpStatusCode.OK);
        unversioned.Content.Headers.ContentType?.MediaType.ShouldContain("application/json");

        var versioned = await client.GetAsync("/api/v1.0/echo");
        versioned.StatusCode.ShouldBe(HttpStatusCode.OK);
        versioned.Content.Headers.ContentType?.MediaType.ShouldContain("application/json");
    }
}
