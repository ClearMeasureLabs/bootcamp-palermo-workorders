using System.Net;
using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.Api;

[TestFixture]
public class EnvironmentStatusEndpointIntegrationTests
{
    private const string ProbeName = "ENV6273_INTEGRATION_PROBE";

    [TearDown]
    public void TearDown()
    {
        Environment.SetEnvironmentVariable(ProbeName, null);
    }

    [Test]
    public async Task Should_Return200AndJson_When_GetEnvironmentStatusUnversioned()
    {
        await using var factory = new EnvironmentStatusWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/status/environment");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        doc.RootElement.TryGetProperty("osDescription", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("processorCount", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("clrVersion", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("frameworkDescription", out _).ShouldBeTrue();
        doc.RootElement.TryGetProperty("environmentVariables", out var envVars).ShouldBeTrue();
        envVars.ValueKind.ShouldBe(JsonValueKind.Array);
    }

    [Test]
    public async Task Should_Return200AndJson_When_GetEnvironmentStatusVersioned()
    {
        await using var factory = new EnvironmentStatusWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1.0/status/environment");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        mediaType.ShouldNotBeNull();
        mediaType!.ShouldContain("application/json");
    }

    [Test]
    public async Task Should_NotExposeSecretVariableValue_When_ProbeIsSet()
    {
        Environment.SetEnvironmentVariable(ProbeName, "super-secret-token-must-not-appear-in-body");
        await using var factory = new EnvironmentStatusWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/status/environment");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var raw = await response.Content.ReadAsStringAsync();
        raw.ShouldNotContain("super-secret-token");
        raw.ShouldNotContain("must-not-appear");

        var payload = JsonSerializer.Deserialize<EnvironmentStatusResponse>(
            raw,
            ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload!.EnvironmentVariables.ShouldContain(e =>
            e.Name == ProbeName && e.IsSet);
    }

    [Test]
    public async Task Should_AllowAnonymous_When_ApiKeyMiddlewareEnabled()
    {
        Environment.SetEnvironmentVariable(ProbeName, null);
        await using var factory = new EnvironmentStatusApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/status/environment");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var versioned = await client.GetAsync("/api/v1.0/status/environment");
        versioned.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
