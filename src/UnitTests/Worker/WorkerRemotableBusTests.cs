using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ChurchBulletin.ServiceDefaults;
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

        await bus.Publish(new TestRemotableEvent());

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

    [Test]
    public async Task Send_WhenNoActivity_ShouldSendCorrelationIdHeaderAsGuid()
    {
        Activity.Current = null;
        var handler = new StubHttpMessageHandler(new TestRemotableResponse("ok"));
        var bus = new RemotableBus(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") }, "api/bus");

        await bus.Send(new TestRemotableRequest());

        handler.LastCorrelationId.ShouldNotBeNullOrWhiteSpace();
        Guid.TryParse(handler.LastCorrelationId, out _).ShouldBeTrue();
    }

    [Test]
    public async Task Send_WhenActivityPresent_ShouldSendActivityTraceIdAsCorrelationId()
    {
        using var activity = new Activity("test");
        activity.Start();
        var handler = new StubHttpMessageHandler(new TestRemotableResponse("ok"));
        var bus = new RemotableBus(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") }, "api/bus");

        await bus.Send(new TestRemotableRequest());

        handler.LastCorrelationId.ShouldBe(activity.TraceId.ToString());
        activity.Stop();
    }

    private sealed class StubHttpMessageHandler(object? responseBody) : HttpMessageHandler
    {
        public string? LastPath { get; private set; }
        public WebServiceMessage? LastPayload { get; private set; }
        public string? LastCorrelationId { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastPath = request.RequestUri?.ToString().TrimStart('/');
            if (request.Headers.TryGetValues(CorrelationIdConstants.HeaderName, out var values))
            {
                LastCorrelationId = values.FirstOrDefault();
            }

            if (request.Content != null)
            {
                LastPayload = await request.Content.ReadFromJsonAsync<WebServiceMessage>(cancellationToken);
            }

            if (responseBody == null)
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("null")
                };
            }

            var message = new WebServiceMessage(responseBody);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(message))
            };
        }
    }

    private record TestRemotableRequest : IRequest<TestRemotableResponse>, IRemotableRequest;

    private record TestRemotableResponse(string Value);

    private record NonRemotableRequest : IRequest<string>;

    private sealed class TestRemotableEvent : IRemotableEvent;

    private sealed class NonRemotableNotification : INotification;
}
