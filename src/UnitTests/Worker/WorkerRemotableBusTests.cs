using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Messaging;
using MediatR;
using Shouldly;
using Worker.Messaging;

namespace ClearMeasure.Bootcamp.UnitTests.Worker;

[TestFixture]
public class WorkerRemotableBusTests
{
    [Test]
    public async Task Send_WhenRemotableRequest_ShouldReturnDeserializedBody()
    {
        var expected = new TestRemotableResponse("ok");
        var handler = new StubHttpMessageHandler(expected);
        var bus = new RemotableBus(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") }, "api/bus");

        var result = await bus.Send(new TestRemotableRequest());

        result.Value.ShouldBe("ok");
        handler.LastPath.ShouldNotBeNull();
        handler.LastPath.ShouldContain("api/bus");
        handler.LastPayload.ShouldNotBeNull();
    }

    [Test]
    public async Task Send_WhenObjectRemotableRequest_ShouldReturnBodyObject()
    {
        var expected = new TestRemotableResponse("object-ok");
        var handler = new StubHttpMessageHandler(expected);
        var bus = new RemotableBus(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") }, "api/bus");

        var result = await bus.Send((object)new TestRemotableRequest());

        result.ShouldBeOfType<TestRemotableResponse>().Value.ShouldBe("object-ok");
    }

    [Test]
    public void Send_WhenNonRemotableRequest_ShouldThrow()
    {
        var bus = new RemotableBus(new HttpClient(new StubHttpMessageHandler(null)), "api/bus");

        Should.Throw<NotSupportedException>(() => bus.Send(new NonRemotableRequest()).GetAwaiter().GetResult())
            .Message.ShouldContain("IRemotableRequest");
    }

    [Test]
    public void SendObject_WhenNonRemotableRequest_ShouldThrow()
    {
        var bus = new RemotableBus(new HttpClient(new StubHttpMessageHandler(null)), "api/bus");

        Should.Throw<NotSupportedException>(() => bus.Send((object)new NonRemotableRequest()).GetAwaiter().GetResult())
            .Message.ShouldContain("IRemotableRequest");
    }

    [Test]
    public async Task Publish_WhenRemotableEvent_ShouldPostMessage()
    {
        var handler = new StubHttpMessageHandler(new TestRemotableResponse("ignored"));
        var bus = new RemotableBus(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") }, "api/bus");

        await bus.Publish(new TestRemotableEvent { Message = "hello" });

        handler.LastPath.ShouldNotBeNull();
        handler.LastPath.ShouldContain("api/bus");
        handler.LastPayload.ShouldNotBeNull();
    }

    [Test]
    public void Publish_WhenNonRemotableEvent_ShouldThrow()
    {
        var bus = new RemotableBus(new HttpClient(new StubHttpMessageHandler(null)), "api/bus");

        Should.Throw<NotSupportedException>(() => bus.Publish(new NonRemotableNotification()).GetAwaiter().GetResult())
            .Message.ShouldContain("IRemotableEvent");
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly object? _responseBody;

        public StubHttpMessageHandler(object? responseBody)
        {
            _responseBody = responseBody;
        }

        public string? LastPath { get; private set; }
        public WebServiceMessage? LastPayload { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastPath = request.RequestUri?.ToString().TrimStart('/');
            if (request.Content != null)
            {
                LastPayload = await request.Content.ReadFromJsonAsync<WebServiceMessage>(cancellationToken);
            }

            if (_responseBody == null)
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("null")
                };
            }

            var message = new WebServiceMessage(_responseBody);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(message))
            };
        }
    }

    private record TestRemotableRequest : IRequest<TestRemotableResponse>, IRemotableRequest;

    private record TestRemotableResponse(string Value);

    private record NonRemotableRequest : IRequest<string>;

    private sealed class TestRemotableEvent : IRemotableEvent
    {
        public string Message { get; set; } = string.Empty;
    }

    private sealed class NonRemotableNotification : INotification;
}
