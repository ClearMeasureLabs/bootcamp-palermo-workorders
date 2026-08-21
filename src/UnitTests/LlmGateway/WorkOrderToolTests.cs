using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.LlmGateway;
using MediatR;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.LlmGateway;

[TestFixture]
public class WorkOrderToolTests
{
    [Test]
    public async Task GetWorkOrderByNumber_ShouldSendQueryAndReturnWorkOrder()
    {
        var expected = new WorkOrder { Number = "WO-001", Title = "Fix pipe" };
        var bus = new StubBus();
        bus.SetResponse<WorkOrderByNumberQuery, WorkOrder?>(expected);
        var tool = new WorkOrderTool(bus);

        var result = await tool.GetWorkOrderByNumber("WO-001");

        result.ShouldBe(expected);
        bus.LastRequest.ShouldBeOfType<WorkOrderByNumberQuery>()
            .Number.ShouldBe("WO-001");
    }

    [Test]
    public async Task GetAllEmployees_ShouldSendQueryAndReturnEmployees()
    {
        var expected = new[]
        {
            new Employee("tlovejoy", "Ted", "Lovejoy", "ted@example.com"),
            new Employee("gwillie", "Groundskeeper", "Willie", "willie@example.com")
        };
        var bus = new StubBus();
        bus.SetResponse<EmployeeGetAllQuery, Employee[]>(expected);
        var tool = new WorkOrderTool(bus);

        var result = await tool.GetAllEmployees();

        result.ShouldBe(expected);
        bus.LastRequest.ShouldBeOfType<EmployeeGetAllQuery>();
    }

    private sealed class StubBus : IBus
    {
        private readonly Dictionary<Type, object?> _responses = new();

        public object? LastRequest { get; private set; }

        public void SetResponse<TRequest, TResponse>(TResponse response)
            where TRequest : IRequest<TResponse>
        {
            _responses[typeof(TRequest)] = response;
        }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
        {
            LastRequest = request;
            if (_responses.TryGetValue(request.GetType(), out var response))
            {
                return Task.FromResult((TResponse)response!);
            }

            throw new InvalidOperationException($"No stub response for {request.GetType().Name}");
        }

        public Task<object?> Send(object request) =>
            throw new NotImplementedException();

        public Task Publish(INotification notification) => Task.CompletedTask;
    }
}
