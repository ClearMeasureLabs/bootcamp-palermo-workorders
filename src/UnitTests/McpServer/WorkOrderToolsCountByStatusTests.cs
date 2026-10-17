using System.Text.Json;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.McpServer.Tools;
using MediatR;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.McpServer;

[TestFixture]
public class WorkOrderToolsCountByStatusTests
{
    [Test]
    public async Task GetWorkOrderCountsByStatus_ShouldReturnSerializedStatusCounts()
    {
        var expected = WorkOrderStatus.GetAllItems().ToDictionary(s => s.Key, _ => 0);
        expected[WorkOrderStatus.Draft.Key] = 2;
        expected[WorkOrderStatus.Assigned.Key] = 1;
        expected[WorkOrderStatus.InProgress.Key] = 3;
        var bus = new StubBus(expected);

        var result = await WorkOrderTools.GetWorkOrderCountsByStatus(bus);

        bus.LastRequest.ShouldBeOfType<WorkOrderCountByStatusQuery>();
        var parsed = JsonSerializer.Deserialize<Dictionary<string, int>>(result);
        parsed.ShouldNotBeNull();
        parsed.ShouldBe(expected);
    }

    private sealed class StubBus(Dictionary<string, int> counts) : IBus
    {
        public object? LastRequest { get; private set; }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
        {
            LastRequest = request;
            if (request is WorkOrderCountByStatusQuery)
            {
                return Task.FromResult((TResponse)(object)counts);
            }

            throw new InvalidOperationException($"No stub response for {request.GetType().Name}");
        }

        public Task<object?> Send(object request) => throw new NotImplementedException();

        public Task Publish(INotification notification) => Task.CompletedTask;
    }
}
