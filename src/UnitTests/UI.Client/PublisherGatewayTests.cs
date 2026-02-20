using System.Net;
using System.Text.Json;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.DataAccess.Messaging;
using ClearMeasure.Bootcamp.UI.Client;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Client;

[TestFixture]
public class PublisherGatewayTests
{
    [Test]
    public async Task Publish_WithRemotableRequest_WrapsInWebServiceMessageAndSendsToTopic()
    {
        var stubGateway = new StubPublisherGateway();
        var request = new TestRemotableRequest { Data = "test" };

        await stubGateway.Publish(request);

        stubGateway.LastMessage.ShouldNotBeNull();
        stubGateway.LastMessage!.GetBodyObject().ShouldBeOfType<TestRemotableRequest>();
        ((TestRemotableRequest)stubGateway.LastMessage.GetBodyObject()).Data.ShouldBe("test");
    }

    [Test]
    public async Task Publish_WithRemotableRequest_ReturnsSendToTopicResult()
    {
        var stubGateway = new StubPublisherGateway();
        var expectedResponse = new WebServiceMessage(new TestRemotableRequest { Data = "response" });
        stubGateway.SetSendToTopicResponse(expectedResponse);
        var request = new TestRemotableRequest { Data = "test" };

        var result = await stubGateway.Publish(request);

        result.ShouldBe(expectedResponse);
    }

    [Test]
    public async Task Publish_WithRemotableEvent_WrapsInWebServiceMessageAndSendsToTopic()
    {
        var stubGateway = new StubPublisherGateway();
        var remotableEvent = new TestRemotableEvent { Message = "event data" };

        await stubGateway.Publish(remotableEvent);

        stubGateway.LastMessage.ShouldNotBeNull();
        stubGateway.LastMessage!.GetBodyObject().ShouldBeOfType<TestRemotableEvent>();
        ((TestRemotableEvent)stubGateway.LastMessage.GetBodyObject()).Message.ShouldBe("event data");
    }

    [Test]
    public async Task SendToTopic_WithValidMessage_PostsToCorrectUrl()
    {
        var stubHandler = new StubHttpMessageHandler();
        var responseMessage = new WebServiceMessage(new TestRemotableRequest { Data = "resp" });
        stubHandler.SetResponse(JsonSerializer.Serialize(responseMessage));
        var httpClient = new HttpClient(stubHandler) { BaseAddress = new Uri("http://localhost/") };
        var gateway = new PublisherGateway(httpClient);
        var message = new WebServiceMessage(new TestRemotableRequest { Data = "req" });

        await gateway.SendToTopic(message);

        stubHandler.LastRequestUri.ShouldNotBeNull();
        stubHandler.LastRequestUri!.ToString().ShouldContain(PublisherGateway.ApiRelativeUrl);
    }

    [Test]
    public async Task SendToTopic_WithValidMessage_ReturnsDeserializedWebServiceMessage()
    {
        var stubHandler = new StubHttpMessageHandler();
        var expectedBody = new TestRemotableRequest { Data = "response data" };
        var responseMessage = new WebServiceMessage(expectedBody);
        stubHandler.SetResponse(JsonSerializer.Serialize(responseMessage));
        var httpClient = new HttpClient(stubHandler) { BaseAddress = new Uri("http://localhost/") };
        var gateway = new PublisherGateway(httpClient);
        var message = new WebServiceMessage(new TestRemotableRequest { Data = "req" });

        var result = await gateway.SendToTopic(message);

        result.ShouldNotBeNull();
        result!.GetBodyObject().ShouldBeOfType<TestRemotableRequest>();
        ((TestRemotableRequest)result.GetBodyObject()).Data.ShouldBe("response data");
    }

    private class StubPublisherGateway : PublisherGateway
    {
        private WebServiceMessage? _sendToTopicResponse = new();

        public WebServiceMessage? LastMessage { get; private set; }

        public StubPublisherGateway() : base(new HttpClient())
        {
        }

        public void SetSendToTopicResponse(WebServiceMessage? response)
        {
            _sendToTopicResponse = response;
        }

        public override Task<WebServiceMessage?> SendToTopic(WebServiceMessage message)
        {
            LastMessage = message;
            return Task.FromResult(_sendToTopicResponse);
        }
    }

    private class StubHttpMessageHandler : HttpMessageHandler
    {
        private string _responseContent = "";

        public Uri? LastRequestUri { get; private set; }

        public void SetResponse(string content)
        {
            _responseContent = content;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequestUri = request.RequestUri;
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_responseContent)
            };
            return Task.FromResult(response);
        }
    }

    private class TestRemotableRequest : IRemotableRequest
    {
        public string Data { get; set; } = "";
    }

    private class TestRemotableEvent : IRemotableEvent
    {
        public string Message { get; set; } = "";
    }
}
