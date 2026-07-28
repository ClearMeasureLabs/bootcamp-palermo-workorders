using System.Net;
using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using ClearMeasure.Bootcamp.UI.Shared;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class HelloControllerWebTests
{
    [Test]
    public async Task Get_Should_Return200_And_ValidJson_Without_ApiKey()
    {
        await using var factory = new ApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();

        var unversioned = await client.GetAsync("/api/hello");
        unversioned.StatusCode.ShouldBe(HttpStatusCode.OK);
        await AssertHelloJson(unversioned);

        var versioned = await client.GetAsync("/api/v1.0/hello");
        versioned.StatusCode.ShouldBe(HttpStatusCode.OK);
        await AssertHelloJson(versioned);
    }

    [Test]
    public async Task Get_Should_Return200_With_ApiKeyPresent()
    {
        await using var factory = new ApiKeyProtectedWebApplicationFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(
            ApiKeyConstants.HeaderName,
            ApiKeyProtectedWebApplicationFactory.TestApiKey);

        var response = await client.GetAsync("/api/hello");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        await AssertHelloJson(response);
    }

    private static async Task AssertHelloJson(HttpResponseMessage response)
    {
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/json");
        var body = await response.Content.ReadAsStringAsync();
        var payload = JsonSerializer.Deserialize<HelloResponse>(body);
        payload.ShouldNotBeNull();
        payload!.Message.ShouldBe("Hello, World!");
    }
}
