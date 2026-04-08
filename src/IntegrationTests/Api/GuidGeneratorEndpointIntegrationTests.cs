using System.Net;
using System.Text.Json;
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
    public async Task Should_Return200AndThreeGuids_When_PostUnversionedWithCount3()
    {
        var response = await _client!.PostAsync("/api/tools/guid-generator?count=3", null);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);

        doc.RootElement.TryGetProperty("guids", out var guids).ShouldBeTrue();
        guids.ValueKind.ShouldBe(JsonValueKind.Array);
        guids.GetArrayLength().ShouldBe(3);
        foreach (var el in guids.EnumerateArray())
        {
            Guid.TryParseExact(el.GetString(), "D", out _).ShouldBeTrue();
        }
    }

    [Test]
    public async Task Should_Return200AndOneGuid_When_PostVersionedWithCount1()
    {
        var response = await _client!.PostAsync("/api/v1.0/tools/guid-generator?count=1", null);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        doc.RootElement.TryGetProperty("guids", out var guids).ShouldBeTrue();
        guids.GetArrayLength().ShouldBe(1);
        Guid.TryParseExact(guids[0].GetString(), "D", out _).ShouldBeTrue();
    }
}
