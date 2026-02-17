using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.Events;
using ClearMeasure.Bootcamp.UI.Client;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using ClearMeasure.Bootcamp.UI.Server.Controllers;
using MediatR;
using Shouldly;
using System.Text.Json;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class SingleApiControllerTests
{
    [Test]
    public async Task ShouldReturnSerializedResponseForRemotableRequest()
    {
        var expectedEmployees = new[] { new Employee("hsimpson", "Homer", "Simpson", "homer@test.com") };
        var stubBus = new StubBus();
        stubBus.SetSendResponse(expectedEmployees);
        var controller = new SingleApiController(stubBus);
        var request = new EmployeeGetAllQuery();
        var message = new WebServiceMessage(request);

        var json = await controller.Post(message);

        var responseMessage = JsonSerializer.Deserialize<WebServiceMessage>(json)!;
        var result = (Employee[])responseMessage.GetBodyObject();
        result.Length.ShouldBe(1);
        result[0].UserName.ShouldBe("hsimpson");
    }

    [Test]
    public async Task ShouldPublishAndReturnEmptyMessageForRemotableEvent()
    {
        var stubBus = new StubBus();
        var controller = new SingleApiController(stubBus);
        var loginEvent = new UserLoggedInEvent("testuser");
        var message = new WebServiceMessage(loginEvent);

        var json = await controller.Post(message);

        stubBus.LastPublishedNotification.ShouldBeOfType<UserLoggedInEvent>();
        ((UserLoggedInEvent)stubBus.LastPublishedNotification!).UserName.ShouldBe("testuser");
        var responseMessage = JsonSerializer.Deserialize<WebServiceMessage>(json)!;
        responseMessage.Body.ShouldBeEmpty();
    }

    [Test]
    public void ShouldThrowForNonRemotableObject()
    {
        var stubBus = new StubBus();
        var controller = new SingleApiController(stubBus);
        var message = new WebServiceMessage(new NonRemotableObject { Value = "test" });

        Should.Throw<InvalidOperationException>(async () => await controller.Post(message));
    }

    [Test]
    public async Task ShouldSendBodyObjectToBusForRemotableRequest()
    {
        var stubBus = new StubBus();
        stubBus.SetSendResponse(Array.Empty<Employee>());
        var controller = new SingleApiController(stubBus);
        var request = new EmployeeGetAllQuery();
        var message = new WebServiceMessage(request);

        await controller.Post(message);

        stubBus.LastSentObject.ShouldBeOfType<EmployeeGetAllQuery>();
    }

    [Test]
    public void ShouldThrowWhenBusReturnsNullForRemotableRequest()
    {
        var stubBus = new StubBus();
        stubBus.SetSendResponse(null);
        var controller = new SingleApiController(stubBus);
        var request = new EmployeeGetAllQuery();
        var message = new WebServiceMessage(request);

        Should.Throw<InvalidOperationException>(async () => await controller.Post(message));
    }

    private class StubBus : IBus
    {
        private object? _sendResponse;
        public object? LastSentObject { get; private set; }
        public object? LastPublishedNotification { get; private set; }

        public void SetSendResponse(object? response)
        {
            _sendResponse = response;
        }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
        {
            throw new NotImplementedException();
        }

        public Task<object?> Send(object request)
        {
            LastSentObject = request;
            return Task.FromResult(_sendResponse);
        }

        public Task Publish(INotification notification)
        {
            LastPublishedNotification = notification;
            return Task.CompletedTask;
        }
    }

    public class NonRemotableObject
    {
        public string Value { get; set; } = "";
    }
}
