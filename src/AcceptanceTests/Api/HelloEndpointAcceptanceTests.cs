using System.Net;
using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Shouldly;

namespace ClearMeasure.Bootcamp.AcceptanceTests.Api;

[TestFixture]
public class HelloEndpointAcceptanceTests : AcceptanceTestBase
{
    [Test]
    public async Task Should_Return200_When_BrowserCallsHello()
    {
        var response = await Page.GotoAsync("/api/hello");
        response.ShouldNotBeNull();
        response!.Status.ShouldBe((int)HttpStatusCode.OK);

        var body = await response.TextAsync();
        var payload = JsonSerializer.Deserialize<HelloResponse>(body, ConditionalGetEtag.JsonSerializerOptions);
        payload.ShouldNotBeNull();
        payload!.Message.ShouldBe("Hello, World!");
    }
}
