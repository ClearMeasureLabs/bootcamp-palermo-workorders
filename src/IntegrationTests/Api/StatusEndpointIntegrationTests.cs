using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using ClearMeasure.Bootcamp.UnitTests.UI.Server;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

[TestFixture]
public class StatusEndpointIntegrationTests
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
    public async Task Should_Return200AndJsonWithStatusOk_When_GetStatus()
    {
        var response = await _client!.GetAsync("/api/status");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");
        var payload = await response.Content.ReadFromJsonAsync<StatusResponse>();
        payload.ShouldNotBeNull();
        payload!.Status.ShouldBe("ok");
    }

    [Test]
    public async Task Should_AllowAnonymousAccess_When_GetStatus()
    {
        var response = await _client!.GetAsync("/api/status");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task Should_ReturnConsistentJsonStructure_When_GetStatus()
    {
        var response = await _client!.GetAsync("/api/status");
        var json = await response.Content.ReadAsStringAsync();

        using var document = JsonDocument.Parse(json);
        document.RootElement.EnumerateObject().Count().ShouldBe(1);
        document.RootElement.GetProperty("status").GetString().ShouldBe("ok");

        var payload = JsonSerializer.Deserialize<StatusResponse>(json);
        payload.ShouldNotBeNull();
        payload!.Status.ShouldBe("ok");
    }

    [Test]
    public async Task Should_Return200WithoutApiKey_When_StatusAndMiddlewareEnabled()
    {
        await using var factory = new ApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/status");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<StatusResponse>();
        payload.ShouldNotBeNull();
        payload!.Status.ShouldBe("ok");
    }
}
