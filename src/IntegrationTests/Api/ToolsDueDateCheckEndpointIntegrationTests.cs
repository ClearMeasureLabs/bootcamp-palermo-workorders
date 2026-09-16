using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClearMeasure.Bootcamp.UnitTests.UI.Server;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

[TestFixture]
public class ToolsDueDateCheckEndpointIntegrationTests
{
    private static readonly JsonSerializerOptions JsonOptions =
        new() { PropertyNameCaseInsensitive = true };

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

    [TestCase("/api/tools/due-date-check")]
    [TestCase("/api/v1.0/tools/due-date-check")]
    public async Task Should_ReturnOverdue_When_DateInPast(string pathPrefix)
    {
        var response = await _client!.GetAsync($"{pathPrefix}?date=2000-01-01");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        json.GetProperty("urgency").GetString().ShouldBe("Overdue");
    }

    [Test]
    public async Task Should_ReturnNone_When_DateInFuture()
    {
        var response = await _client!.GetAsync("/api/tools/due-date-check?date=2099-12-31");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        json.GetProperty("urgency").GetString().ShouldBe("None");
    }

    [TestCase("/api/tools/due-date-check")]
    [TestCase("/api/v1.0/tools/due-date-check")]
    public async Task Should_Return400_When_DateMissing(string pathPrefix)
    {
        var response = await _client!.GetAsync(pathPrefix);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [TestCase("/api/tools/due-date-check?date=not-a-date")]
    [TestCase("/api/v1.0/tools/due-date-check?date=01%2F15%2F2025")]
    public async Task Should_Return400_When_DateInvalidFormat(string url)
    {
        var response = await _client!.GetAsync(url);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Should_Return200WithoutApiKey_When_MiddlewareEnabled()
    {
        await using var factory = new ApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var unversioned = await client.GetAsync("/api/tools/due-date-check?date=2000-01-01");
        unversioned.StatusCode.ShouldBe(HttpStatusCode.OK);

        var versioned = await client.GetAsync("/api/v1.0/tools/due-date-check?date=2000-01-01");
        versioned.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
