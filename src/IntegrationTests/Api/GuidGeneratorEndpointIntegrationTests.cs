using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
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
    public async Task Should_Return200Json_When_PostUnversionedWithEmptyBody()
    {
        var response = await _client!.PostAsync("/api/tools/guid-generator", null);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/json");
        await AssertValidGuidResponse(await response.Content.ReadAsStringAsync(), 1);
    }

    [Test]
    public async Task Should_Return200Json_When_PostVersionedWithEmptyBody()
    {
        var response = await _client!.PostAsync("/api/v1.0/tools/guid-generator", null);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/json");
        await AssertValidGuidResponse(await response.Content.ReadAsStringAsync(), 1);
    }

    [Test]
    public async Task Should_ReturnCorrectShape_When_CountSpecified()
    {
        using var content = new StringContent("{\"count\":3}", Encoding.UTF8, "application/json");
        var response = await _client!.PostAsync("/api/tools/guid-generator", content);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        await AssertValidGuidResponse(await response.Content.ReadAsStringAsync(), 3);
    }

    [Test]
    public async Task Should_Return400_When_CountInvalid()
    {
        using var content = new StringContent("{\"count\":-1}", Encoding.UTF8, "application/json");
        var response = await _client!.PostAsync("/api/tools/guid-generator", content);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.TryGetProperty("type", out _).ShouldBeTrue();
        root.TryGetProperty("detail", out _).ShouldBeTrue();
        root.GetProperty("status").GetInt32().ShouldBe(400);
    }

    [Test]
    public async Task Should_Return400_When_CountAboveMax()
    {
        using var content = new StringContent("{\"count\":101}", Encoding.UTF8, "application/json");
        var response = await _client!.PostAsync("/api/v1.0/tools/guid-generator", content);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("status").GetInt32().ShouldBe(400);
    }

    private static Task AssertValidGuidResponse(string json, int expectedCount)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("count").GetInt32().ShouldBe(expectedCount);
        var guids = root.GetProperty("guids");
        guids.GetArrayLength().ShouldBe(expectedCount);
        foreach (var element in guids.EnumerateArray())
        {
            Guid.TryParse(element.GetString(), out _).ShouldBeTrue();
        }

        return Task.CompletedTask;
    }
}
