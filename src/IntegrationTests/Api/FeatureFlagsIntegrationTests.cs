using System.Net;
using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.Data.Sqlite;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

[TestFixture]
public class FeatureFlagsIntegrationTests
{
    private SqliteConnection? _sharedMemoryHold;
    private GrpcWebApplicationFactory? _factory;
    private HttpClient? _httpClient;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _sharedMemoryHold = new SqliteConnection(GrpcWebApplicationFactory.SqliteSharedMemoryConnectionString);
        _sharedMemoryHold.Open();
        _factory = new GrpcWebApplicationFactory();
        _httpClient = _factory.CreateClient();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _httpClient?.Dispose();
        _factory?.Dispose();
        _sharedMemoryHold?.Dispose();
    }

    [TestCase("/api/features/flags")]
    [TestCase("/api/v1.0/features/flags")]
    public async Task Should_ReturnFeatureFlagsJson_When_Get(string url)
    {
        var response = await _httpClient!.GetAsync(url);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/json");
        var json = await response.Content.ReadAsStringAsync();
        var payload = JsonSerializer.Deserialize<Dictionary<string, bool>>(
            json,
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload!.Count.ShouldBe(ApplicationFeatureFlags.All.Count);
        foreach (var kv in ApplicationFeatureFlags.All)
        {
            payload[kv.Key].ShouldBe(kv.Value);
        }
    }
}
