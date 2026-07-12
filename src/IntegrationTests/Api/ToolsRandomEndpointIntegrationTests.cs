using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using ClearMeasure.Bootcamp.UnitTests.UI.Server;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

[TestFixture]
public class ToolsRandomEndpointIntegrationTests
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
    public async Task Should_Return200JsonWithNumber_When_UnversionedRouteAndTypeNumber()
    {
        var response = await _client!.GetAsync("/api/tools/random?type=number");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("type").GetString().ShouldBe("number");
        doc.RootElement.GetProperty("value").TryGetInt32(out var n).ShouldBeTrue();
        n.ShouldBeGreaterThanOrEqualTo(int.MinValue);
        n.ShouldBeLessThanOrEqualTo(int.MaxValue);
    }

    [Test]
    public async Task Should_Return200JsonWithUuid_When_VersionedRouteAndTypeUuid()
    {
        var response = await _client!.GetAsync("/api/v1.0/tools/random?type=uuid");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("type").GetString().ShouldBe("uuid");
        Guid.TryParse(doc.RootElement.GetProperty("value").GetString(), out _).ShouldBeTrue();
    }

    [Test]
    public async Task Should_Return200JsonWithString_When_TypeString()
    {
        const string urlSafeAlphabet =
            "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_";

        var response = await _client!.GetAsync("/api/tools/random?type=string");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("type").GetString().ShouldBe("string");
        var text = doc.RootElement.GetProperty("value").GetString();
        text.ShouldNotBeNull();
        text!.Length.ShouldBe(ToolsRandomController.DefaultStringLength);
        text.All(urlSafeAlphabet.Contains).ShouldBeTrue();
    }

    [Test]
    public async Task Should_Return200JsonWithColor_When_TypeColor()
    {
        var response = await _client!.GetAsync("/api/tools/random?type=color");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("type").GetString().ShouldBe("color");
        var hex = doc.RootElement.GetProperty("value").GetString();
        Regex.IsMatch(hex!, "^#[0-9A-F]{6}$", RegexOptions.None).ShouldBeTrue();
    }

    [Test]
    public async Task Should_Return400ProblemDetails_When_TypeQueryMissing()
    {
        var response = await _client!.GetAsync("/api/tools/random");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/problem+json");

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("status").GetInt32().ShouldBe(400);
        doc.RootElement.GetProperty("detail").GetString().ShouldNotBeNullOrEmpty();
    }

    [Test]
    public async Task Should_Return400ProblemDetails_When_TypeUnsupported()
    {
        var response = await _client!.GetAsync("/api/tools/random?type=invalid");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("status").GetInt32().ShouldBe(400);
        var detail = doc.RootElement.GetProperty("detail").GetString();
        detail.ShouldNotBeNull();
        detail!.ShouldContain("invalid");
        detail.ShouldContain("number");
    }

    [Test]
    public async Task Should_Return200WithoutApiKey_When_MiddlewareEnabled()
    {
        await using var factory = new ApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var unversioned = await client.GetAsync("/api/tools/random?type=number");
        unversioned.StatusCode.ShouldBe(HttpStatusCode.OK);

        var versioned = await client.GetAsync("/api/v1.0/tools/random?type=number");
        versioned.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task Should_ReturnDistinctValues_When_CalledTwiceWithSameType()
    {
        var first = await _client!.GetAsync("/api/tools/random?type=string");
        var second = await _client.GetAsync("/api/tools/random?type=string");

        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        second.StatusCode.ShouldBe(HttpStatusCode.OK);

        using var firstDoc = JsonDocument.Parse(await first.Content.ReadAsStringAsync());
        using var secondDoc = JsonDocument.Parse(await second.Content.ReadAsStringAsync());
        var firstValue = firstDoc.RootElement.GetProperty("value").GetString();
        var secondValue = secondDoc.RootElement.GetProperty("value").GetString();
        firstValue.ShouldNotBe(secondValue);
    }
}
