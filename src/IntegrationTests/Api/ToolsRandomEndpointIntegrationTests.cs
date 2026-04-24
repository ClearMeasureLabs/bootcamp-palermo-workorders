using System.Globalization;
using System.Net;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UnitTests.UI.Server;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

[TestFixture]
public class ToolsRandomEndpointIntegrationTests
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
    public async Task Should_Return200AndWellFormedNumber_When_TypeIsNumber()
    {
        var response = await _client!.GetAsync("/api/tools/random?type=number");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        int.TryParse(body, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n).ShouldBeTrue();
        n.ShouldBeGreaterThanOrEqualTo(int.MinValue);
        n.ShouldBeLessThanOrEqualTo(int.MaxValue);

        var v = await _client.GetAsync("/api/v1.0/tools/random?type=number");
        v.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task Should_Return200AndWellFormedString_When_TypeIsString()
    {
        var response = await _client!.GetAsync("/api/tools/random?type=string");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Length.ShouldBe(ToolsRandomController.RandomStringLength);
        body.ToCharArray().All(c => char.IsAsciiLetterOrDigit(c)).ShouldBeTrue();
    }

    [Test]
    public async Task Should_Return200AndValidGuid_When_TypeIsUuid()
    {
        var response = await _client!.GetAsync("/api/tools/random?type=uuid");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        Guid.TryParse(body, out var g).ShouldBeTrue();
        g.ShouldNotBe(Guid.Empty);
    }

    [Test]
    public async Task Should_Return200AndRecognizedColor_When_TypeIsColor()
    {
        var response = await _client!.GetAsync("/api/tools/random?type=color");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Length.ShouldBe(7);
        body[0].ShouldBe('#');
        body[1..].All(c => char.IsAsciiHexDigitUpper(c)).ShouldBeTrue();
    }

    [Test]
    public async Task Should_Return400_When_TypeIsMissingOrEmpty()
    {
        var missing = await _client!.GetAsync("/api/tools/random");
        missing.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var empty = await _client.GetAsync("/api/tools/random?type=");
        empty.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Should_Return400_When_TypeIsUnknown()
    {
        var response = await _client!.GetAsync("/api/tools/random?type=not-a-real-type");
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Should_TreatTypeParameterCaseInsensitively_When_Number()
    {
        var a = await _client!.GetAsync("/api/tools/random?type=NUMBER");
        var b = await _client.GetAsync("/api/tools/random?type=number");
        a.StatusCode.ShouldBe(HttpStatusCode.OK);
        b.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task Should_NotRequireApiKey_When_MiddlewareEnabled()
    {
        await using var factory = new DiagnosticsApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var withoutKey = await client.GetAsync("/api/tools/random?type=uuid");
        withoutKey.StatusCode.ShouldBe(HttpStatusCode.OK);

        var withoutKeyV = await client.GetAsync("/api/v1.0/tools/random?type=uuid");
        withoutKeyV.StatusCode.ShouldBe(HttpStatusCode.OK);

        var diagnostics = await client.GetAsync("/api/diagnostics");
        diagnostics.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        using var withKey = factory.CreateClient();
        withKey.DefaultRequestHeaders.Add(
            ApiKeyConstants.HeaderName,
            ApiKeyProtectedWebApplicationFactory.TestApiKey);
        var okDiag = await withKey.GetAsync("/api/diagnostics");
        okDiag.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task Should_ReturnDifferentValues_When_TwoSequentialGets()
    {
        var first = await _client!.GetAsync("/api/tools/random?type=uuid");
        var second = await _client.GetAsync("/api/tools/random?type=uuid");
        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        second.StatusCode.ShouldBe(HttpStatusCode.OK);
        var a = await first.Content.ReadAsStringAsync();
        var b = await second.Content.ReadAsStringAsync();
        a.ShouldNotBe(b);
    }
}
