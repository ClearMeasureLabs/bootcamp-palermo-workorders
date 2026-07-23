using System.Net;
using ClearMeasure.Bootcamp.UnitTests.UI.Server;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

[TestFixture]
public class HealthzEndpointIntegrationTests
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
    public async Task Should_Return200_When_GetUnversioned()
    {
        var response = await _client!.GetAsync("/api/healthz");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).ShouldBeEmpty();
    }

    [Test]
    public async Task Should_Return200_When_GetVersioned()
    {
        var response = await _client!.GetAsync("/api/v1.0/healthz");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).ShouldBeEmpty();
        response.Headers.TryGetValues("api-supported-versions", out var values).ShouldBeTrue();
        values.ShouldNotBeNull();
        string.Join(", ", values!).ShouldContain("1.0");
    }

    [Test]
    public async Task Should_Return200WithoutApiKey_When_ApiKeyMiddlewareEnabled()
    {
        await using var factory = new ApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var unversioned = await client.GetAsync("/api/healthz");
        unversioned.StatusCode.ShouldBe(HttpStatusCode.OK);

        var versioned = await client.GetAsync("/api/v1.0/healthz");
        versioned.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
