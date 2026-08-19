using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

[TestFixture]
public class HashEndpointIntegrationTests
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
    public async Task Should_Return200AndValidJson_When_PostToUnversionedPath()
    {
        var response = await PostHashAsync("/api/tools/hash", "simple text");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        await AssertHashResponseShape(response);
    }

    [Test]
    public async Task Should_Return200AndValidJson_When_PostToVersionedPath()
    {
        var response = await PostHashAsync("/api/v1.0/tools/hash", "simple text");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        await AssertHashResponseShape(response);
    }

    [Test]
    public async Task Should_Return400BadRequest_When_TextMissing()
    {
        var response = await _client!.PostAsync(
            "/api/tools/hash",
            new StringContent("{}", Encoding.UTF8, "application/json"));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        var hasDetail = root.TryGetProperty("detail", out var detail) && detail.GetString()?.Length > 0;
        var hasTitle = root.TryGetProperty("title", out var title) && title.GetString()?.Length > 0;
        (hasDetail || hasTitle).ShouldBeTrue();
    }

    [Test]
    public async Task Should_ReturnConsistentDigests_When_SameTextPostedTwice()
    {
        const string path = "/api/tools/hash";
        const string text = "repeat-me";

        var first = await PostHashAsync(path, text);
        var second = await PostHashAsync(path, text);

        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        second.StatusCode.ShouldBe(HttpStatusCode.OK);

        var firstPayload = await ReadHashResponse(first);
        var secondPayload = await ReadHashResponse(second);

        firstPayload!.Sha256.ShouldBe(secondPayload!.Sha256);
        firstPayload.Md5.ShouldBe(secondPayload.Md5);
        firstPayload.Sha1.ShouldBe(secondPayload.Sha1);
    }

    [Test]
    public async Task Should_ReturnKnownVector_When_TextIsHello()
    {
        var response = await PostHashAsync("/api/tools/hash", "hello");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var payload = await ReadHashResponse(response);
        payload!.Sha256.ShouldBe("2cf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b9824");
    }

    private Task<HttpResponseMessage> PostHashAsync(string path, string text) =>
        _client!.PostAsync(
            path,
            new StringContent(
                JsonSerializer.Serialize(new { text }),
                Encoding.UTF8,
                "application/json"));

    private static async Task AssertHashResponseShape(HttpResponseMessage response)
    {
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");

        var payload = await ReadHashResponse(response);
        payload.ShouldNotBeNull();
        payload!.Sha256.Length.ShouldBe(64);
        payload.Md5.Length.ShouldBe(32);
        payload.Sha1.Length.ShouldBe(40);
    }

    private static async Task<HashResponse?> ReadHashResponse(HttpResponseMessage response)
    {
        await using var stream = await response.Content.ReadAsStreamAsync();
        return await JsonSerializer.DeserializeAsync<HashResponse>(
            stream,
            ConditionalGetEtag.JsonSerializerOptions);
    }
}
