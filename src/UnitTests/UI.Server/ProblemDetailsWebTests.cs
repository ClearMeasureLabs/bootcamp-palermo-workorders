using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class ProblemDetailsWebTests
{
    [Test]
    public async Task Should_ReturnProblemJson_When_ApiVersionUnsupported()
    {
        await using var factory = new ApiVersioningRoutingWebApplicationFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v2.0/health");

        response.IsSuccessStatusCode.ShouldBeFalse();
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/problem+json");

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.TryGetProperty("status", out var statusProp).ShouldBeTrue();
        statusProp.GetInt32().ShouldBe((int)response.StatusCode);
        doc.RootElement.TryGetProperty("title", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("type", out _).ShouldBeTrue();
    }

    [Test]
    public async Task Should_ReturnProblemJson_When_ApiRouteNotFound()
    {
        await using var factory = new ApiVersioningRoutingWebApplicationFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1.0/does-not-exist-" + Guid.NewGuid().ToString("N"));

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/problem+json");

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("status").GetInt32().ShouldBe(StatusCodes.Status404NotFound);
    }
}
