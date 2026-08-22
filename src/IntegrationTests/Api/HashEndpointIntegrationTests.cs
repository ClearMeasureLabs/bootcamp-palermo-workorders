using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

[TestFixture]
public class HashEndpointIntegrationTests
{
    private const string AbcSha256 = "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad";

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
    public async Task Should_Return200AndSha256_When_PostUnversioned()
    {
        using var content = JsonContent.Create(new { text = "abc" });

        var response = await _client!.PostAsync("/api/tools/hash", content);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("sha256").GetString().ShouldBe(AbcSha256);
    }

    [Test]
    public async Task Should_Return200AndSha256_When_PostVersioned()
    {
        using var content = JsonContent.Create(new { text = "abc" });

        var response = await _client!.PostAsync("/api/v1.0/tools/hash", content);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("sha256").GetString().ShouldBe(AbcSha256);
    }

    [Test]
    public async Task Should_Return400_When_BodyOmitsText()
    {
        using var content = JsonContent.Create(new { includeMd5 = true });

        var response = await _client!.PostAsync("/api/tools/hash", content);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
