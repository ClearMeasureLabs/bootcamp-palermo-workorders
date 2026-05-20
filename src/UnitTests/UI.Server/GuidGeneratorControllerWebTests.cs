using System.Net;
using System.Net.Http.Json;
using System.Text;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public sealed class GuidGeneratorControllerWebTests
{
    private ApiVersioningRoutingWebApplicationFactory? _factory;

    [OneTimeSetUp]
    public void OneTimeSetUp() => _factory = new ApiVersioningRoutingWebApplicationFactory();

    [OneTimeTearDown]
    public void OneTimeTearDown() => _factory?.Dispose();

    [Test]
    public async Task Should_ReturnOneGuid_When_PostWithNoBody()
    {
        using var client = _factory!.CreateClient();
        var response = await client.PostAsync("/api/tools/guid-generator", null);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<GuidGeneratorResponse>();
        dto.ShouldNotBeNull();
        dto!.Count.ShouldBe(1);
        dto.Guids.Count.ShouldBe(1);
        Guid.TryParse(dto.Guids[0], out _).ShouldBeTrue();
    }

    [Test]
    public async Task Should_ReturnOneGuid_When_PostWithEmptyJsonObject()
    {
        using var client = _factory!.CreateClient();
        using var content = new StringContent("{}", Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/api/tools/guid-generator", content);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<GuidGeneratorResponse>();
        dto!.Count.ShouldBe(1);
        dto.Guids.Count.ShouldBe(1);
    }

    [Test]
    public async Task Should_ReturnDistinctGuids_When_CountIsThree()
    {
        using var client = _factory!.CreateClient();
        var response = await client.PostAsync("/api/tools/guid-generator?count=3", null);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<GuidGeneratorResponse>();
        dto!.Count.ShouldBe(3);
        dto.Guids.Count.ShouldBe(3);
        dto.Guids.Distinct().Count().ShouldBe(3);
        dto.Guids.All(g => g.Length == 36 && g.Count(c => c == '-') == 4).ShouldBeTrue();
    }

    [Test]
    public async Task Should_MatchUnversioned_When_PostedToVersionedRoute()
    {
        using var client = _factory!.CreateClient();
        var unversioned = await client.PostAsync("/api/tools/guid-generator?count=2", null);
        var versioned = await client.PostAsync("/api/v1.0/tools/guid-generator?count=2", null);

        unversioned.StatusCode.ShouldBe(HttpStatusCode.OK);
        versioned.StatusCode.ShouldBe(HttpStatusCode.OK);
        var a = await unversioned.Content.ReadFromJsonAsync<GuidGeneratorResponse>();
        var b = await versioned.Content.ReadFromJsonAsync<GuidGeneratorResponse>();
        a!.Count.ShouldBe(2);
        b!.Count.ShouldBe(2);
    }

    [Test]
    public async Task Should_UseQueryCount_When_BothQueryAndBodyPresent()
    {
        using var client = _factory!.CreateClient();
        using var content = JsonContent.Create(new { count = 2 });
        var response = await client.PostAsync("/api/tools/guid-generator?count=5", content);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<GuidGeneratorResponse>();
        dto!.Count.ShouldBe(5);
        dto.Guids.Count.ShouldBe(5);
    }

    [Test]
    public async Task Should_Return400_When_CountIsZero()
    {
        using var client = _factory!.CreateClient();
        var response = await client.PostAsync("/api/tools/guid-generator?count=0", null);
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Should_Return400_When_CountExceedsMax()
    {
        using var client = _factory!.CreateClient();
        var response = await client.PostAsync("/api/tools/guid-generator?count=101", null);
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Should_Return400_When_CountIsNegative()
    {
        using var client = _factory!.CreateClient();
        var response = await client.PostAsync("/api/tools/guid-generator?count=-1", null);
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Should_Return400OrUnprocessable_When_CountIsNotIntegerInQuery()
    {
        using var client = _factory!.CreateClient();
        var response = await client.PostAsync("/api/tools/guid-generator?count=abc", null);
        (response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.UnprocessableEntity)
            .ShouldBeTrue();
    }
}
