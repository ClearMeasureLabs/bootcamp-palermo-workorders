using System.Net;
using System.Net.Http.Json;
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
    public async Task Should_Return200AndOneGuid_When_PostWithoutParameters()
    {
        var response = await _client!.PostAsync("/api/tools/guid-generator", null);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<GuidGeneratorResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        payload.ShouldNotBeNull();
        payload!.Guids.Count.ShouldBe(1);
        Guid.TryParse(payload.Guids[0], out _).ShouldBeTrue();
    }

    [Test]
    public async Task Should_Return200AndManyGuids_When_QueryCountProvided()
    {
        var response = await _client!.PostAsync("/api/tools/guid-generator?count=7", null);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<GuidGeneratorResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        payload.ShouldNotBeNull();
        payload!.Guids.Count.ShouldBe(7);
        payload.Guids.Select(g => Guid.Parse(g)).Distinct().Count().ShouldBe(7);
    }

    [Test]
    public async Task Should_Return200_When_VersionedRouteUsed()
    {
        var response = await _client!.PostAsync("/api/v1.0/tools/guid-generator?count=2", null);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<GuidGeneratorResponse>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        payload!.Guids.Count.ShouldBe(2);
    }

    [Test]
    public async Task Should_Return400_When_CountExceedsMax()
    {
        var response = await _client!.PostAsync("/api/tools/guid-generator?count=101", null);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Should_Return400_When_CountBelowOne()
    {
        var response = await _client!.PostAsync("/api/tools/guid-generator?count=0", null);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
