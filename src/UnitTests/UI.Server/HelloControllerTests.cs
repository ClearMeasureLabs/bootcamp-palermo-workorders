using System.Net;
using System.Text.Json;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class HelloControllerTests
{
    private ApiVersioningRoutingWebApplicationFactory? _factory;
    private HttpClient? _client;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _factory = new ApiVersioningRoutingWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    [Test]
    public async Task Should_Return200_When_GetHello_V1Path()
    {
        var response = await _client!.GetAsync("/api/v1.0/hello");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");
    }

    [Test]
    public async Task Should_ReturnCorrectMessage_When_GetHello()
    {
        var response = await _client!.GetAsync("/api/v1.0/hello");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("message").GetString().ShouldBe("Hello, World!");
    }

    [Test]
    public async Task Should_ReturnNotSuccess_When_GetHello_UnsupportedVersion()
    {
        var response = await _client!.GetAsync("/api/v2.0/hello");

        response.IsSuccessStatusCode.ShouldBeFalse();
        response.StatusCode.ShouldBeOneOf(HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Should_IncludeSupportedVersionsHeader_When_GetHello()
    {
        var response = await _client!.GetAsync("/api/v1.0/hello");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.TryGetValues("api-supported-versions", out var values).ShouldBeTrue();
        values.ShouldNotBeNull();
        string.Join(", ", values!).ShouldContain("1.0");
    }
}
