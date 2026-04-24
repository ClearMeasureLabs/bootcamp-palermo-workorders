using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

[TestFixture]
public class GuidGeneratorEndpointIntegrationTests
{
    private DetailedHealthWebApplicationFactory? _factory;
    private HttpClient? _client;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _factory = new DetailedHealthWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    [Test]
    public async Task Should_Return200AndOneGuid_When_PostWithoutBody()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/tools/guid-generator")
        {
            Content = new StringContent("", Encoding.UTF8, "application/json")
        };

        var response = await _client!.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<GuidGeneratorResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        payload.ShouldNotBeNull();
        payload!.Guids.Count.ShouldBe(1);
        Guid.TryParseExact(payload.Guids[0], "D", out _).ShouldBeTrue();
    }

    [Test]
    public async Task Should_Return200AndRequestedCount_When_PostWithJsonBody()
    {
        var response = await _client!.PostAsJsonAsync("/api/tools/guid-generator", new { count = 5 });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<GuidGeneratorResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        payload.ShouldNotBeNull();
        payload!.Guids.Count.ShouldBe(5);
        payload.Guids.Select(Guid.Parse).Distinct().Count().ShouldBe(5);
    }

    [Test]
    public async Task Should_Return400_When_CountTooLarge()
    {
        var response = await _client!.PostAsJsonAsync("/api/tools/guid-generator", new { count = 101 });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Should_Return200_When_PostVersionedRoute()
    {
        var response = await _client!.PostAsJsonAsync("/api/v1.0/tools/guid-generator", new { count = 2 });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<GuidGeneratorResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        payload!.Guids.Count.ShouldBe(2);
    }
}
