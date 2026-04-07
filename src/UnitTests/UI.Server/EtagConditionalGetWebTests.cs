using System.Net;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

/// <summary>
/// ETag conditional GET against UI.Server in-process (SQLite memory); avoids IntegrationTests assembly SetUpFixture / LocalDB.
/// </summary>
[TestFixture]
public class EtagConditionalGetWebTests
{
    private ApiVersioningRoutingWebApplicationFactory? _factory;

    [OneTimeSetUp]
    public void OneTimeSetUp() => _factory = new ApiVersioningRoutingWebApplicationFactory();

    [OneTimeTearDown]
    public void OneTimeTearDown() => _factory?.Dispose();

    [Test]
    public async Task Should_Return304NotModified_When_VersionIfNoneMatchMatchesEtag()
    {
        using var client = _factory!.CreateClient();
        var first = await client.GetAsync("/api/version");
        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        var etag = first.Headers.ETag;
        etag.ShouldNotBeNull();

        using var second = new HttpRequestMessage(HttpMethod.Get, "/api/version");
        second.Headers.IfNoneMatch.Add(etag!);
        var notModified = await client.SendAsync(second);
        notModified.StatusCode.ShouldBe(HttpStatusCode.NotModified);
        (await notModified.Content.ReadAsByteArrayAsync()).Length.ShouldBe(0);
    }

    [Test]
    public async Task Should_IncludeEtagHeader_When_GetSimpleHealth()
    {
        using var client = _factory!.CreateClient();
        var response = await client.GetAsync("/api/health");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.ETag.ShouldNotBeNull();
    }

    [Test]
    public async Task Should_Return304NotModified_When_IfNoneMatchMatchesEtag_OnDetailedHealth()
    {
        using var client = _factory!.CreateClient();
        var first = await client.GetAsync("/api/health/detailed");
        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        var etag = first.Headers.ETag;
        etag.ShouldNotBeNull();

        using var second = new HttpRequestMessage(HttpMethod.Get, "/api/health/detailed");
        second.Headers.IfNoneMatch.Add(etag!);
        var notModified = await client.SendAsync(second);
        notModified.StatusCode.ShouldBe(HttpStatusCode.NotModified);
        (await notModified.Content.ReadAsByteArrayAsync()).Length.ShouldBe(0);
    }
}
