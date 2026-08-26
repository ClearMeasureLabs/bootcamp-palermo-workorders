using System.Net;
using System.Net.Http.Json;
using ClearMeasure.Bootcamp.UnitTests.UI.Server;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

[TestFixture]
public class ToolsGuidGeneratorEndpointIntegrationTests
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
    public async Task Should_Return200AndGuidArray_When_PostUnversioned_DefaultCount()
    {
        var response = await _client!.PostAsync("/api/tools/guid-generator", null);

        await AssertGuidArrayAsync(response, expectedCount: 1);
    }

    [Test]
    public async Task Should_Return200AndGuidArray_When_PostVersioned_WithCount()
    {
        var response = await _client!.PostAsync("/api/v1.0/tools/guid-generator?count=3", null);

        await AssertGuidArrayAsync(response, expectedCount: 3);
    }

    [TestCase("/api/tools/guid-generator?count=0")]
    [TestCase("/api/tools/guid-generator?count=101")]
    [TestCase("/api/v1.0/tools/guid-generator?count=0")]
    [TestCase("/api/v1.0/tools/guid-generator?count=101")]
    public async Task Should_Return400_When_CountOutOfRange(string path)
    {
        var response = await _client!.PostAsync(path, null);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Should_Return200WithoutApiKey_When_MiddlewareEnabled()
    {
        await using var factory = new ApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var unversioned = await client.PostAsync("/api/tools/guid-generator", null);
        await AssertGuidArrayAsync(unversioned, expectedCount: 1);

        var versioned = await client.PostAsync("/api/v1.0/tools/guid-generator?count=2", null);
        await AssertGuidArrayAsync(versioned, expectedCount: 2);
    }

    private static async Task AssertGuidArrayAsync(HttpResponseMessage response, int expectedCount)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var guids = await response.Content.ReadFromJsonAsync<string[]>();
        guids.ShouldNotBeNull();
        guids.Length.ShouldBe(expectedCount);
        foreach (var guid in guids)
        {
            Guid.TryParseExact(guid, "D", out _).ShouldBeTrue();
        }
    }
}
