using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class RequestBodyBufferingWebTests
{
    [Test]
    public async Task Should_ReturnSameBodyTwice_When_PostToBufferProbeAndBufferingEnabled()
    {
        await using var factory = new ApiVersioningRoutingWebApplicationFactory();
        var client = factory.CreateClient();
        const string payload = """{"test":"buffer"}""";

        var response = await client.PostAsync(
            "/_test/body-buffer-probe",
            new StringContent(payload, System.Text.Encoding.UTF8, "application/json"));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var node = await response.Content.ReadFromJsonAsync<JsonObject>();
        node.ShouldNotBeNull();
        node["first"]!.GetValue<string>().ShouldBe(payload);
        node["second"]!.GetValue<string>().ShouldBe(payload);
    }

    [Test]
    public async Task Should_NotBufferSecondRead_When_BufferingDisabled()
    {
        await using var factory = new RequestBodyBufferingDisabledWebApplicationFactory();
        var client = factory.CreateClient();
        const string payload = """{"x":1}""";

        var response = await client.PostAsync(
            "/_test/body-buffer-probe",
            new StringContent(payload, System.Text.Encoding.UTF8, "application/json"));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var node = await response.Content.ReadFromJsonAsync<JsonObject>();
        node.ShouldNotBeNull();
        node["first"]!.GetValue<string>().ShouldBe(payload);
        node["second"]!.GetValue<string>().ShouldBe(string.Empty);
    }
}
