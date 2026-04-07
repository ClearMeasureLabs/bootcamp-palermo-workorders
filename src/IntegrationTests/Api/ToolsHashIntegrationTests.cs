using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

[TestFixture]
public class ToolsHashIntegrationTests
{
    private SqliteConnection? _sharedMemoryHold;
    private ToolsHashWebApplicationFactory? _factory;
    private HttpClient? _httpClient;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _sharedMemoryHold = new SqliteConnection(ToolsHashWebApplicationFactory.SqliteConnectionString);
        _sharedMemoryHold.Open();
        _factory = new ToolsHashWebApplicationFactory();
        _httpClient = _factory.CreateClient();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _httpClient?.Dispose();
        _factory?.Dispose();
        _sharedMemoryHold?.Dispose();
    }

    [Test]
    public async Task Should_ReturnSha256Hex_When_PostJsonText_V1Path()
    {
        using var content = new StringContent(
            """{"text":"hello"}""",
            Encoding.UTF8,
            "application/json");

        var response = await _httpClient!.PostAsync(
            new Uri(_httpClient.BaseAddress!, "api/v1.0/tools/hash"),
            content);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("algorithm").GetString().ShouldBe("SHA-256");
        root.GetProperty("hashHex").GetString()
            .ShouldBe("2cf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b9824");
    }

    [Test]
    public async Task Should_ReturnEmptyStringHash_When_TextOmitted()
    {
        using var content = new StringContent("{}", Encoding.UTF8, "application/json");

        var response = await _httpClient!.PostAsync(
            new Uri(_httpClient.BaseAddress!, "api/tools/hash"),
            content);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("hashHex").GetString()
            .ShouldBe("e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855");
    }

    [Test]
    public async Task Should_Return400_When_BodyMissing()
    {
        using var content = new StringContent("", Encoding.UTF8, "application/json");

        var response = await _httpClient!.PostAsync(
            new Uri(_httpClient.BaseAddress!, "api/v1.0/tools/hash"),
            content);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
