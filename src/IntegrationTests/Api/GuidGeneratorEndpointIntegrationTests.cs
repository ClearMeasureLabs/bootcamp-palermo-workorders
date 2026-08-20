using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClearMeasure.Bootcamp.UnitTests.UI.Server;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

[TestFixture]
public class GuidGeneratorEndpointIntegrationTests
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
    public async Task Should_Return200AndUniqueGuids_When_PostUnversioned()
    {
        var response = await _client!.PostAsync("/api/tools/guid-generator", null);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var guids = await ReadGuidArrayAsync(response);
        guids.Length.ShouldBe(1);
        guids.Distinct().Count().ShouldBe(guids.Length);
        foreach (var g in guids)
            Guid.TryParseExact(g, "D", out _).ShouldBeTrue();
    }

    [Test]
    public async Task Should_Return200AndUniqueGuids_When_PostVersioned()
    {
        var response = await _client!.PostAsync("/api/v1.0/tools/guid-generator?count=3", null);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var guids = await ReadGuidArrayAsync(response);
        guids.Length.ShouldBe(3);
        guids.Distinct().Count().ShouldBe(3);
        foreach (var g in guids)
            Guid.TryParseExact(g, "D", out _).ShouldBeTrue();
    }

    [Test]
    public async Task Should_Return400_When_CountOutOfRange()
    {
        var tooLow = await _client!.PostAsync("/api/tools/guid-generator?count=0", null);
        tooLow.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var tooHigh = await _client.PostAsync("/api/v1.0/tools/guid-generator?count=101", null);
        tooHigh.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Should_Return200WithoutApiKey_When_MiddlewareEnabled()
    {
        await using var factory = new ApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var unversioned = await client.PostAsync("/api/tools/guid-generator", null);
        unversioned.StatusCode.ShouldBe(HttpStatusCode.OK);
        var guids = await ReadGuidArrayAsync(unversioned);
        guids.Length.ShouldBe(1);

        var versioned = await client.PostAsync("/api/v1.0/tools/guid-generator?count=2", null);
        versioned.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await ReadGuidArrayAsync(versioned)).Length.ShouldBe(2);
    }

    private static async Task<string[]> ReadGuidArrayAsync(HttpResponseMessage response)
    {
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("json");
        var guids = await response.Content.ReadFromJsonAsync<string[]>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        guids.ShouldNotBeNull();
        return guids!;
    }
}
