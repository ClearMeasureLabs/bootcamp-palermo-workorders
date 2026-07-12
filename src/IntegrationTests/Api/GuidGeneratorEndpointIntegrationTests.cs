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
    public async Task Should_Return200WithOneGuid_When_PostUnversionedWithEmptyBody()
    {
        var response = await _client!.PostAsync(
            "/api/tools/guid-generator",
            JsonContent.Create(new { }));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");

        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        document.RootElement.GetProperty("count").GetInt32().ShouldBe(1);
        var guids = document.RootElement.GetProperty("guids");
        guids.GetArrayLength().ShouldBe(1);
        Guid.TryParse(guids[0].GetString(), out _).ShouldBeTrue();
    }

    [Test]
    public async Task Should_Return200WithMultipleGuids_When_CountSpecifiedOnUnversionedRoute()
    {
        var response = await _client!.PostAsync(
            "/api/tools/guid-generator",
            JsonContent.Create(new { count = 3 }));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        document.RootElement.GetProperty("count").GetInt32().ShouldBe(3);
        var guids = document.RootElement.GetProperty("guids");
        guids.GetArrayLength().ShouldBe(3);
        for (var i = 0; i < 3; i++)
        {
            Guid.TryParse(guids[i].GetString(), out _).ShouldBeTrue();
        }
    }

    [Test]
    public async Task Should_Return200WithJsonGuids_When_PostVersionedRoute()
    {
        var response = await _client!.PostAsync(
            "/api/v1.0/tools/guid-generator",
            JsonContent.Create(new { count = 2 }));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        document.RootElement.GetProperty("count").GetInt32().ShouldBe(2);
        document.RootElement.GetProperty("guids").GetArrayLength().ShouldBe(2);
    }

    [Test]
    public async Task Should_Return400WithProblemDetails_When_CountExceedsMaximum()
    {
        var response = await _client!.PostAsync(
            "/api/tools/guid-generator",
            JsonContent.Create(new { count = 101 }));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");

        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        document.RootElement.GetProperty("status").GetInt32().ShouldBe(400);
        document.RootElement.GetProperty("title").GetString().ShouldNotBeNullOrEmpty();
        document.RootElement.GetProperty("type").GetString().ShouldNotBeNullOrEmpty();
        document.RootElement.GetProperty("detail").GetString()!
            .ShouldContain("count must be between 1 and 100");
    }

    [Test]
    public async Task Should_Return400WithProblemDetails_When_CountBelowMinimum()
    {
        var response = await _client!.PostAsync(
            "/api/tools/guid-generator",
            JsonContent.Create(new { count = 0 }));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        document.RootElement.GetProperty("status").GetInt32().ShouldBe(400);
        document.RootElement.GetProperty("detail").GetString()!
            .ShouldContain("count must be between 1 and 100");
    }

    [Test]
    public async Task Should_Return200WithoutApiKey_When_MiddlewareEnabled()
    {
        await using var factory = new ApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var unversioned = await client.PostAsync(
            "/api/tools/guid-generator",
            JsonContent.Create(new { }));
        unversioned.StatusCode.ShouldBe(HttpStatusCode.OK);

        var versioned = await client.PostAsync(
            "/api/v1.0/tools/guid-generator",
            JsonContent.Create(new { }));
        versioned.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
