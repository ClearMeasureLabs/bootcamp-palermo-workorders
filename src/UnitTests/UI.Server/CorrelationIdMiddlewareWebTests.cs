using System.Net;
using ClearMeasure.Bootcamp.ServiceDefaults;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class CorrelationIdMiddlewareWebTests
{
    [Test]
    public async Task Should_ReturnGeneratedCorrelationIdInHeader_When_RequestHasNoCorrelationHeader()
    {
        await using var factory = new ApiVersioningRoutingWebApplicationFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1.0/health");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.TryGetValues(CorrelationIdConstants.HeaderName, out var values).ShouldBeTrue();
        var id = values!.Single();
        id.Length.ShouldBeGreaterThan(0);
        Guid.TryParse(id, out _).ShouldBeTrue();
    }

    [Test]
    public async Task Should_EchoCorrelationIdInResponse_When_RequestProvidesValidHeader()
    {
        await using var factory = new ApiVersioningRoutingWebApplicationFactory();
        var client = factory.CreateClient();
        const string expected = "test-correlation-abc";
        client.DefaultRequestHeaders.Add(CorrelationIdConstants.HeaderName, expected);

        var response = await client.GetAsync("/api/v1.0/health");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.GetValues(CorrelationIdConstants.HeaderName).Single().ShouldBe(expected);
    }

    [Test]
    public async Task Should_IgnoreOversizedCorrelationHeader_When_HeaderExceedsMaxLength()
    {
        await using var factory = new ApiVersioningRoutingWebApplicationFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(CorrelationIdConstants.HeaderName, new string('x', 129));

        var response = await client.GetAsync("/api/v1.0/health");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var returned = response.Headers.GetValues(CorrelationIdConstants.HeaderName).Single();
        returned.Length.ShouldBe(36);
        Guid.TryParse(returned, out _).ShouldBeTrue();
    }
}
