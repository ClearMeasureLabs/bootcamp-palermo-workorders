using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Messaging;
using ClearMeasure.Bootcamp.UI.Client;
using MediatR;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Client;

[TestFixture]
public class RemotableBusTests
{
    [Test]
    public void Should_SendRemoteableRequest_CallsGateway()
    {
        var stubMediator = new StubMediator();
        var stubGateway = new StubPublisherGateway();
        var expectedResponse = "Remote test";
        stubGateway.SetResponse(expectedResponse);
        var bus = new RemotableBus(stubMediator, stubGateway);
        var request = new TestRemotableRequest();

        var result = bus.Send(request).Result;

        result.ShouldBe(expectedResponse);
        stubGateway.LastRequest.ShouldBe(request);
        stubMediator.LastRequest.ShouldBeNull();
    }

    [Test]
    public void Should_SendNonRemoteableRequest_CallsMediator()
    {
        var stubMediator = new StubMediator();
        var stubGateway = new StubPublisherGateway();
        var expectedResponse = "mediator response";
        stubMediator.SetResponse(expectedResponse);
        var bus = new RemotableBus(stubMediator, stubGateway);
        var request = new NonRemotableTestRequest();

        var result = bus.Send(request).Result;

        result.ShouldBe(expectedResponse);
        stubMediator.LastRequest.ShouldBe(request);
        stubGateway.LastRequest.ShouldBeNull();
    }

    [Test]
    public void Should_SendObjectRequest_CallsMediator()
    {
        var stubMediator = new StubMediator();
        var stubGateway = new StubPublisherGateway();
        var expectedResponse = "object response";
        stubMediator.SetObjectResponse(expectedResponse);
        var bus = new RemotableBus(stubMediator, stubGateway);
        var request = new TestCommand();

        var result = bus.Send(request).Result;

        result.ShouldBe(expectedResponse);
        stubMediator.LastObjectRequest.ShouldBe(request);
    }

    [Test]
    public async Task Should_PublishNotification_CallsMediator()
    {
        var stubMediator = new StubMediator();
        var stubGateway = new StubPublisherGateway();
        var bus = new RemotableBus(stubMediator, stubGateway);
        var notification = new TestNotification { Message = "test message" };

        await bus.Publish(notification);

        stubMediator.LastNotification.ShouldBe(notification);
        stubGateway.LastRequest.ShouldBeNull();
    }

    [Test]
    public async Task Should_PublishRemotableEvent_CallsGateway()
    {
        var stubMediator = new StubMediator();
        var stubGateway = new StubPublisherGateway();
        var bus = new RemotableBus(stubMediator, stubGateway);
        var remotableEvent = new TestRemotableEvent { Message = "remote event" };

        await bus.Publish(remotableEvent);

        stubGateway.LastRequest.ShouldBeOfType<TestRemotableEvent>();
        ((TestRemotableEvent)stubGateway.LastRequest!).Message.ShouldBe("remote event");
        stubMediator.LastNotification.ShouldBeNull();
    }

    [Test]
    public void Should_ThrowException_WhenGatewayReturnsNull()
    {
        var stubMediator = new StubMediator();
        var stubGateway = new StubPublisherGateway();
        stubGateway.SetResponse<object>(null);
        var bus = new RemotableBus(stubMediator, stubGateway);
        var request = new ForecastQuery();

        Should.Throw<Exception>(() => bus.Send(request).Result);
    }

    private class StubMediator : IMediator
    {
        private object? _response;
        private object? _objectResponse;

        public object? LastRequest { get; private set; }
        public object? LastObjectRequest { get; private set; }
        public object? LastNotification { get; private set; }

        public void SetResponse<T>(T response)
        {
            _response = response;
        }

        public void SetObjectResponse(object? response)
        {
            _objectResponse = response;
        }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request,
            CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return Task.FromResult((TResponse)_response!);
        }

        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
        {
            LastObjectRequest = request;
            return Task.FromResult(_objectResponse);
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
            where TRequest : IRequest
        {
            return Task.CompletedTask;
        }

        public Task Publish(object notification, CancellationToken cancellationToken = default)
        {
            LastNotification = notification;
            return Task.CompletedTask;
        }

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification
        {
            LastNotification = notification;
            return Task.CompletedTask;
        }

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }

    private class StubPublisherGateway : PublisherGateway
    {
        private object? _response;

        public object? LastRequest { get; private set; }

        public StubPublisherGateway() : base(new HttpClient())
        {
        }

        public void SetResponse<T>(T? response)
        {
            _response = response;
        }

        public override Task<WebServiceMessage?> SendToTopic(WebServiceMessage message)
        {
            LastRequest = message.GetBodyObject();
            if (_response == null)
            {
                return Task.FromResult<WebServiceMessage?>(null);
            }

            var responseMessage = new WebServiceMessage(_response);
            return Task.FromResult<WebServiceMessage?>(responseMessage);
        }
    }

    private record NonRemotableTestRequest : IRequest<string>
    {
        public string Data { get; set; } = "non-remotable";
    }

    private record TestRemotableRequest : IRequest<string>, IRemotableRequest
    {
        public string Data { get; set; } = "non-remotable";
    }

    private class TestCommand
    {
        public string Data { get; set; } = "test data";
    }

    private class TestNotification : INotification
    {
        public string Message { get; set; } = string.Empty;
    }

    private class TestRemotableEvent : IRemotableEvent
    {
        public string Message { get; set; } = string.Empty;
    }
}