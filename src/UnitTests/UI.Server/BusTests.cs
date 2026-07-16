using System.Diagnostics;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.UI.Shared;
using MediatR;
using Shouldly;

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

    [Test]
    public void Should_SendRequest_WithTelemetryTaggedProperty_AddsTagToActivity()
    {
        var capturedActivities = new List<Activity>();
        using var listener = CreateBusActivityListener(capturedActivities);
        var stubMediator = new StubMediator();
        stubMediator.SetResponse("response");
        var bus = new Bus(stubMediator);
        var request = new TaggedTestQuery("WO-123", "sensitive prompt text", new Employee("jdoe", "Jane", "Doe", "jane@example.com"));

        bus.Send(request).Wait();

        var activity = capturedActivities.ShouldHaveSingleItem();
        activity.GetTagItem("bus.message.WorkOrderNumber").ShouldBe("WO-123");
    }

    [Test]
    public void Should_SendRequest_WithUnmarkedProperties_DoesNotAddTagsToActivity()
    {
        var capturedActivities = new List<Activity>();
        using var listener = CreateBusActivityListener(capturedActivities);
        var stubMediator = new StubMediator();
        stubMediator.SetResponse("response");
        var bus = new Bus(stubMediator);
        var request = new TaggedTestQuery("WO-123", "sensitive prompt text", new Employee("jdoe", "Jane", "Doe", "jane@example.com"));

        bus.Send(request).Wait();

        var activity = capturedActivities.ShouldHaveSingleItem();
        activity.GetTagItem("bus.message.Prompt").ShouldBeNull();
        activity.GetTagItem("bus.message.CurrentUser").ShouldBeNull();
    }

    [Test]
    public void Should_SendRequest_WithLongTaggedValue_TruncatesTagTo128Characters()
    {
        var capturedActivities = new List<Activity>();
        using var listener = CreateBusActivityListener(capturedActivities);
        var stubMediator = new StubMediator();
        stubMediator.SetResponse("response");
        var bus = new Bus(stubMediator);
        var longValue = new string('x', 500);
        var request = new TaggedTestQuery(longValue, "prompt", null);

        bus.Send(request).Wait();

        var activity = capturedActivities.ShouldHaveSingleItem();
        activity.GetTagItem("bus.message.WorkOrderNumber").ShouldBe(new string('x', 128));
    }

    [Test]
    public void Should_SendRequest_WithNullTaggedValue_AddsEmptyStringTag()
    {
        var capturedActivities = new List<Activity>();
        using var listener = CreateBusActivityListener(capturedActivities);
        var stubMediator = new StubMediator();
        stubMediator.SetResponse("response");
        var bus = new Bus(stubMediator);
        var request = new TaggedTestQuery(null, "prompt", null);

        bus.Send(request).Wait();

        var activity = capturedActivities.ShouldHaveSingleItem();
        activity.GetTagItem("bus.message.WorkOrderNumber").ShouldBe(string.Empty);
    }

    private static ActivityListener CreateBusActivityListener(List<Activity> capturedActivities)
    {
        var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "ChurchBulletin.Application.Bus",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = capturedActivities.Add
        };
        ActivitySource.AddActivityListener(listener);
        return listener;
    }

    private record TaggedTestQuery(
        [property: TelemetryTag] string? WorkOrderNumber,
        string Prompt,
        Employee? CurrentUser) : IRequest<string>;

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