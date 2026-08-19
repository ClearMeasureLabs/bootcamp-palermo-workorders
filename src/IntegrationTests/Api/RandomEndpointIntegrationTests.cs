using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using ClearMeasure.Bootcamp.UnitTests.UI.Server;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

[TestFixture]
public partial class RandomEndpointIntegrationTests
{
    [GeneratedRegex("^[a-zA-Z0-9]+$")]
    private static partial Regex AlphanumericRegex();

    [GeneratedRegex("^#[0-9A-Fa-f]{6}$")]
    private static partial Regex HexColorRegex();

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
    public async Task Should_Return200JsonWithRandomNumber_When_GetUnversioned()
    {
        var response = await _client!.GetAsync("/api/tools/random?type=number");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");
        var payload = await DeserializeResponse(response);
        payload.Type.ShouldBe("number");
        payload.Value.ValueKind.ShouldBe(JsonValueKind.Number);
    }

    [Test]
    public async Task Should_Return200JsonWithRandomNumber_When_GetVersioned()
    {
        var response = await _client!.GetAsync("/api/v1.0/tools/random?type=number");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");
        var payload = await DeserializeResponse(response);
        payload.Type.ShouldBe("number");
        payload.Value.ValueKind.ShouldBe(JsonValueKind.Number);
    }

    [Test]
    public async Task Should_Return200JsonWithRandomString_When_QueryParamSpecified()
    {
        var response = await _client!.GetAsync("/api/tools/random?type=string&length=20");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var payload = await DeserializeResponse(response);
        payload.Type.ShouldBe("string");
        payload.Value.GetString()!.Length.ShouldBe(20);
        AlphanumericRegex().IsMatch(payload.Value.GetString()!).ShouldBeTrue();
    }

    [Test]
    public async Task Should_Return200JsonWithRandomUuid_When_TypeIsUuid()
    {
        var response = await _client!.GetAsync("/api/tools/random?type=uuid");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var payload = await DeserializeResponse(response);
        payload.Type.ShouldBe("uuid");
        Guid.TryParse(payload.Value.GetString(), out _).ShouldBeTrue();
    }

    [Test]
    public async Task Should_Return200JsonWithHexColor_When_TypeIsColor()
    {
        var response = await _client!.GetAsync("/api/tools/random?type=color");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var payload = await DeserializeResponse(response);
        payload.Type.ShouldBe("color");
        HexColorRegex().IsMatch(payload.Value.GetString()!).ShouldBeTrue();
    }

    [Test]
    public async Task Should_Return400BadRequest_When_MissingTypeParam()
    {
        var response = await _client!.GetAsync("/api/tools/random");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var error = await DeserializeError(response);
        error.Error.ShouldBe("type parameter required");
    }

    [Test]
    public async Task Should_Return400BadRequest_When_InvalidTypeParam()
    {
        var response = await _client!.GetAsync("/api/tools/random?type=unknown");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var error = await DeserializeError(response);
        error.Error.ShouldContain("number");
    }

    [Test]
    public async Task Should_Return200WithoutApiKey_When_ApiKeyMiddlewareEnabledAndRouteIsPublic_Unversioned()
    {
        await using var factory = new ApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/tools/random?type=number");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task Should_Return200WithoutApiKey_When_ApiKeyMiddlewareEnabledAndRouteIsPublic_Versioned()
    {
        await using var factory = new ApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1.0/tools/random?type=number");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    private static async Task<TestRandomValueResponse> DeserializeResponse(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        var payload = JsonSerializer.Deserialize<TestRandomValueResponse>(json, ConditionalGetEtag.JsonSerializerOptions);
        return payload.ShouldNotBeNull();
    }

    private static async Task<RandomErrorResponse> DeserializeError(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        var payload = JsonSerializer.Deserialize<RandomErrorResponse>(json, ConditionalGetEtag.JsonSerializerOptions);
        return payload.ShouldNotBeNull();
    }

    private sealed record TestRandomValueResponse(string Type, JsonElement Value);
}
