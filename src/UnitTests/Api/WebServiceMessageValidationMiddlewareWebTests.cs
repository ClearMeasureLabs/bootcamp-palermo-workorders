using System.Net;
using System.Net.Http.Json;
using System.Text;
using ClearMeasure.Bootcamp.Core.Messaging;
using ClearMeasure.Bootcamp.Core.Model.Events;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Client;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Api;

[TestFixture]
public class WebServiceMessageValidationMiddlewareWebTests
{
    [Test]
    public async Task Should_Return400_When_WorkOrderByNumberQueryHasEmptyNumber()
    {
        await using var factory = new WebServiceMessageValidationWebApplicationFactory();
        using var client = factory.CreateClient();

        var message = new WebServiceMessage(new WorkOrderByNumberQuery(""));
        var response = await PostSingleApiAsync(client, message);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var json = await response.Content.ReadAsStringAsync();
        json.ShouldContain("errors");
    }

    [Test]
    public async Task Should_Return200_When_UserLoggedInEventIsValid()
    {
        await using var factory = new WebServiceMessageValidationWebApplicationFactory();
        using var client = factory.CreateClient();

        var message = new WebServiceMessage(new UserLoggedInEvent("testuser"));
        var response = await PostSingleApiAsync(client, message);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task Should_Return200_When_WebServiceMessageJsonUsesCamelCase_LikePostAsJsonAsync()
    {
        await using var factory = new WebServiceMessageValidationWebApplicationFactory();
        using var client = factory.CreateClient();

        var message = new WebServiceMessage(new UserLoggedInEvent("testuser"));
        var response = await client.PostAsJsonAsync(PublisherGateway.ApiRelativeUrlV1, message);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    private static async Task<HttpResponseMessage> PostSingleApiAsync(
        HttpClient client,
        WebServiceMessage message)
    {
        var json = message.GetJson();
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await client.PostAsync(PublisherGateway.ApiRelativeUrlV1, content);
    }
}
