using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UnitTests.UI.Server;
using Microsoft.AspNetCore.Mvc.Testing;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

[TestFixture]
public sealed class GuidGeneratorIntegrationTests
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
    public async Task Should_Return200AndOneGuid_When_PostUnversionedWithEmptyBody()
    {
        var response = await _client!.PostAsync(
            "/api/tools/guid-generator",
            new StringContent("{}", Encoding.UTF8, "application/json"));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType.ShouldContain("application/json");

        var guids = await ReadGuidsAsync(response);
        guids.Count.ShouldBe(1);
        _ = Guid.Parse(guids[0]);
    }

    [Test]
    public async Task Should_Return200AndDistinctGuids_When_PostWithCount()
    {
        var response = await _client!.PostAsJsonAsync("/api/tools/guid-generator", new { count = 5 });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var guids = await ReadGuidsAsync(response);
        guids.Count.ShouldBe(5);
        var set = new HashSet<string>(StringComparer.Ordinal);
        foreach (var g in guids)
        {
            set.Add(g).ShouldBeTrue();
            _ = Guid.Parse(g);
        }
    }

    [Test]
    public async Task Should_Return200_When_CountAtMaximumBoundary()
    {
        var response = await _client!.PostAsJsonAsync("/api/tools/guid-generator", new { count = 100 });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var guids = await ReadGuidsAsync(response);
        guids.Count.ShouldBe(100);
        foreach (var g in guids)
        {
            _ = Guid.Parse(g);
        }
    }

    [TestCase(0)]
    [TestCase(-3)]
    [TestCase(101)]
    public async Task Should_Return400_When_CountOutOfRange(int count)
    {
        var response = await _client!.PostAsJsonAsync("/api/tools/guid-generator", new { count });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        doc.RootElement.TryGetProperty("detail", out _).ShouldBeTrue();
    }

    [Test]
    public async Task Should_Return415_When_InvalidContentTypeForJsonBinding()
    {
        using var content = new StringContent("{}", Encoding.UTF8, "text/plain");
        var response = await _client!.PostAsync("/api/tools/guid-generator", content);

        response.StatusCode.ShouldBe(HttpStatusCode.UnsupportedMediaType);
    }

    [Test]
    public async Task Should_Return400_When_InvalidJsonBody()
    {
        var response = await _client!.PostAsync(
            "/api/tools/guid-generator",
            new StringContent("{ not json", Encoding.UTF8, "application/json"));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Should_Return200WithEmptyBody_When_PostWithoutBody()
    {
        var response = await _client!.PostAsync("/api/tools/guid-generator", null);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var guids = await ReadGuidsAsync(response);
        guids.Count.ShouldBe(1);
        _ = Guid.Parse(guids[0]);
    }

    [Test]
    public async Task Should_ExposeSameContract_When_PostVersionedRoute()
    {
        var unversioned = await _client!.PostAsJsonAsync("/api/tools/guid-generator", new { count = 2 });
        unversioned.StatusCode.ShouldBe(HttpStatusCode.OK);
        var uGuids = await ReadGuidsAsync(unversioned);
        uGuids.Count.ShouldBe(2);

        var versioned = await _client!.PostAsJsonAsync("/api/v1.0/tools/guid-generator", new { count = 2 });
        versioned.StatusCode.ShouldBe(HttpStatusCode.OK);
        var vGuids = await ReadGuidsAsync(versioned);
        vGuids.Count.ShouldBe(2);
    }

    [Test]
    public async Task Should_EnforceApiKey_When_MiddlewareEnabled()
    {
        await using var factory = new DiagnosticsApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var unauth = await client.PostAsJsonAsync("/api/tools/guid-generator", new { });
        unauth.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        var unauthVersioned = await client.PostAsJsonAsync("/api/v1.0/tools/guid-generator", new { });
        unauthVersioned.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        using var withKeyClient = factory.CreateClient();
        withKeyClient.DefaultRequestHeaders.Add(
            ApiKeyConstants.HeaderName,
            ApiKeyProtectedWebApplicationFactory.TestApiKey);

        var ok = await withKeyClient.PostAsJsonAsync("/api/tools/guid-generator", new { count = 3 });
        ok.StatusCode.ShouldBe(HttpStatusCode.OK);
        var guids = await ReadGuidsAsync(ok);
        guids.Count.ShouldBe(3);

        var okVersioned = await withKeyClient.PostAsJsonAsync("/api/v1.0/tools/guid-generator", new { count = 1 });
        okVersioned.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await ReadGuidsAsync(okVersioned)).Count.ShouldBe(1);
    }

    private static async Task<List<string>> ReadGuidsAsync(HttpResponseMessage response)
    {
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        var arr = doc.RootElement.GetProperty("guids");
        var list = new List<string>();
        foreach (var el in arr.EnumerateArray())
        {
            list.Add(el.GetString().ShouldNotBeNull());
        }

        return list;
    }

}
