using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClearMeasure.Bootcamp.UnitTests.UI.Server;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

[TestFixture]
public class ToolsSlugifyEndpointIntegrationTests
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
    public async Task Should_Return200WithSlug_When_PostUnversioned()
    {
        var response = await _client!.PostAsJsonAsync("/api/tools/slugify", new { text = "Hello World!" });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        json.GetProperty("slug").GetString().ShouldBe("hello-world");
    }

    [Test]
    public async Task Should_Return200WithSlug_When_PostVersioned()
    {
        var response = await _client!.PostAsJsonAsync("/api/v1.0/tools/slugify", new { text = "Foo---Bar" });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        json.GetProperty("slug").GetString().ShouldBe("foo-bar");
    }

    [TestCase("/api/tools/slugify")]
    [TestCase("/api/v1.0/tools/slugify")]
    public async Task Should_Return400_When_TextMissing(string path)
    {
        var missingField = await _client!.PostAsJsonAsync(path, new { });
        missingField.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var nullText = await _client!.PostAsJsonAsync(path, new { text = (string?)null });
        nullText.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Should_Return200WithoutApiKey_When_MiddlewareEnabled()
    {
        await using var factory = new ApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var unversioned = await client.PostAsJsonAsync("/api/tools/slugify", new { text = "test" });
        unversioned.StatusCode.ShouldBe(HttpStatusCode.OK);

        var versioned = await client.PostAsJsonAsync("/api/v1.0/tools/slugify", new { text = "test" });
        versioned.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
