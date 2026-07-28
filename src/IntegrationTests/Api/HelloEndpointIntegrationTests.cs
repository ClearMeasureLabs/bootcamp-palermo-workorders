using System.Net;
using System.Net.Http.Json;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
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
    public async Task Should_Return200AndJson_When_GetHelloEndpoint()
    {
        var response = await _client!.GetAsync("/api/hello");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldBe("application/json");
    }

    [Test]
    public async Task Should_DeserializeToHelloResponse_When_GetHelloEndpoint()
    {
        var response = await _client!.GetAsync("/api/hello");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<HelloResponse>();
        payload.ShouldNotBeNull();
        payload!.Message.ShouldBe("Hello, World!");
    }
}
