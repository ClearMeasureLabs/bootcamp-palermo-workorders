using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ClearMeasure.Bootcamp.UnitTests.UI.Server;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

[TestFixture]
public class GuidGeneratorEndpointIntegrationTests
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
    public async Task Should_Return200AndJsonGuids_When_PostUnversioned()
    {
        using var content = JsonContent.Create(new { count = 3 });
        var response = await _client!.PostAsync("/api/tools/guid-generator", content);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/json");
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("guids").GetArrayLength().ShouldBe(3);
    }

    [Test]
    public async Task Should_Return200AndJsonGuids_When_PostVersioned()
    {
        using var content = JsonContent.Create(new { count = 3 });
        var response = await _client!.PostAsync("/api/v1.0/tools/guid-generator", content);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/json");
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("guids").GetArrayLength().ShouldBe(3);
    }

    [Test]
    public async Task Should_Return200WithOneGuid_When_EmptyJsonBody()
    {
        using var emptyObject = new StringContent("{}", Encoding.UTF8, "application/json");
        var response = await _client!.PostAsync("/api/tools/guid-generator", emptyObject);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("guids").GetArrayLength().ShouldBe(1);
    }

    [Test]
    public async Task Should_Return400_When_CountOutOfRange()
    {
        using var zero = JsonContent.Create(new { count = 0 });
        var zeroResponse = await _client!.PostAsync("/api/tools/guid-generator", zero);
        zeroResponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        using var overMax = JsonContent.Create(new { count = 101 });
        var overMaxResponse = await _client.PostAsync("/api/tools/guid-generator", overMax);
        overMaxResponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Should_Return200WithoutApiKey_When_MiddlewareEnabled()
    {
        await using var factory = new ApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        using var content = JsonContent.Create(new { count = 2 });

        var unversioned = await client.PostAsync("/api/tools/guid-generator", content);
        unversioned.StatusCode.ShouldBe(HttpStatusCode.OK);
        using (var doc = JsonDocument.Parse(await unversioned.Content.ReadAsStringAsync()))
        {
            doc.RootElement.GetProperty("guids").GetArrayLength().ShouldBe(2);
        }

        using var versionedContent = JsonContent.Create(new { count = 2 });
        var versioned = await client.PostAsync("/api/v1.0/tools/guid-generator", versionedContent);
        versioned.StatusCode.ShouldBe(HttpStatusCode.OK);
        using (var doc = JsonDocument.Parse(await versioned.Content.ReadAsStringAsync()))
        {
            doc.RootElement.GetProperty("guids").GetArrayLength().ShouldBe(2);
        }
    }
}
