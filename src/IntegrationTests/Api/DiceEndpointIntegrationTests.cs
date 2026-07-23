using System.Net;
using ClearMeasure.Bootcamp.UnitTests.UI.Server;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

[TestFixture]
public class DiceEndpointIntegrationTests
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
    public async Task Should_Return200AndPlainTextRoll_When_GetUnversioned()
    {
        var response = await _client!.GetAsync("/api/dice");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("text/plain");
        var body = await response.Content.ReadAsStringAsync();
        int.TryParse(body, out var value).ShouldBeTrue();
        value.ShouldBeInRange(1, 6);
    }

    [Test]
    public async Task Should_Return200AndPlainTextRoll_When_GetVersioned()
    {
        var response = await _client!.GetAsync("/api/v1.0/dice");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("text/plain");
        var body = await response.Content.ReadAsStringAsync();
        int.TryParse(body, out var value).ShouldBeTrue();
        value.ShouldBeInRange(1, 6);
    }

    [Test]
    public async Task Should_Return200WithoutApiKey_When_DiceAndMiddlewareEnabled()
    {
        await using var factory = new ApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var unversioned = await client.GetAsync("/api/dice");
        unversioned.StatusCode.ShouldBe(HttpStatusCode.OK);
        int.TryParse(await unversioned.Content.ReadAsStringAsync(), out var unversionedValue).ShouldBeTrue();
        unversionedValue.ShouldBeInRange(1, 6);

        var versioned = await client.GetAsync("/api/v1.0/dice");
        versioned.StatusCode.ShouldBe(HttpStatusCode.OK);
        int.TryParse(await versioned.Content.ReadAsStringAsync(), out var versionedValue).ShouldBeTrue();
        versionedValue.ShouldBeInRange(1, 6);
    }
}
