using MediatR;
using ClearMeasure.Bootcamp.UI.Server;
using Shouldly;
using ClearMeasure.Bootcamp.UI.Shared;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class BusTests
{
    [Test]
    public void Should_SendRequest_WithGenericResponse_CallsMediator()
    {
        var stubMediator = new StubMediator();
        var expectedResponse = "Test";
        stubMediator.SetResponse(expectedResponse);
        var bus = new Bus(stubMediator);
        var request = new TestQuery();

        var result = bus.Send(request).Result;

        result.ShouldBe(expectedResponse);
        stubMediator.LastRequest.ShouldBe(request);
    }

    [Test]
    public void Should_SendObjectRequest_CallsMediator()
    {
        var stubMediator = new StubMediator();
        var expectedResponse = "test response";
        stubMediator.SetObjectResponse(expectedResponse);
        var bus = new Bus(stubMediator);
        var request = new TestCommand();

        var result = bus.Send(request).Result;

        result.ShouldBe(expectedResponse);
        stubMediator.LastObjectRequest.ShouldBe(request);
    }

    [Test]
    public void Should_SendObjectRequest_WithNullResponse_CallsMediator()
    {
        var stubMediator = new StubMediator();
        stubMediator.SetObjectResponse(null);
        var bus = new Bus(stubMediator);
        var request = new TestCommand();

        var result = bus.Send(request).Result;

        result.ShouldBeNull();
        stubMediator.LastObjectRequest.ShouldBe(request);
    }

    [Test]
    public void Should_PublishNotification_CallsMediator()
    {
        var stubMediator = new StubMediator();
        var bus = new Bus(stubMediator);
        var notification = new TestNotification { Message = "test message" };

        bus.Publish(notification);

        stubMediator.LastNotification.ShouldBe(notification);
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

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = new())
            where TRequest : IRequest
        {
            throw new NotImplementedException();
        }

        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
        {
            LastObjectRequest = request;
            return Task.FromResult(_objectResponse);
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

    private class TestCommand
    {
        public string Data { get; set; } = "test data";
    }

    private class TestNotification : INotification
    {
        public string Message { get; set; } = string.Empty;
    }

    private record TestQuery : IRequest<string>
    {
    }
}