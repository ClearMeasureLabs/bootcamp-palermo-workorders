using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClearMeasure.Bootcamp.UnitTests.UI.Server;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

[TestFixture]
public class ToolsWordCountEndpointIntegrationTests
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

    [Test]
    public async Task Should_Return200WithCounts_When_PostUnversioned()
    {
        var response = await _client!.PostAsJsonAsync("/api/tools/word-count", new { text = "hello world" });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        json.GetProperty("wordCount").GetInt32().ShouldBe(2);
        json.GetProperty("characterCount").GetInt32().ShouldBe(11);
        json.GetProperty("lineCount").GetInt32().ShouldBe(1);
    }

    [Test]
    public async Task Should_Return200WithCounts_When_PostVersioned()
    {
        var response = await _client!.PostAsJsonAsync("/api/v1.0/tools/word-count", new { text = "one\ntwo" });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        json.GetProperty("wordCount").GetInt32().ShouldBe(2);
        json.GetProperty("lineCount").GetInt32().ShouldBe(2);
    }

    [TestCase("/api/tools/word-count")]
    [TestCase("/api/v1.0/tools/word-count")]
    public async Task Should_Return400_When_TextMissing(string path)
    {
        var missingField = await _client!.PostAsJsonAsync(path, new { });
        missingField.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var nullText = await _client!.PostAsJsonAsync(path, new { text = (string?)null });
        nullText.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
