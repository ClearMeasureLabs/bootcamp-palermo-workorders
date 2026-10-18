using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.McpServer.Tools;
using MediatR;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.McpServer;

[TestFixture]
public class WorkOrderToolsListOverdueTests
{
    [Test]
    public async Task ShouldSerializeOverdueWorkOrdersOldestFirst()
    {
        var creator = new Employee("u1", "Ann", "Lee", "a@test.com");
        var older = new WorkOrder
        {
            Creator = creator,
            Number = "OD1",
            Title = "Old",
            Status = WorkOrderStatus.Draft,
            DueDate = new DateOnly(2020, 1, 1)
        };
        var newer = new WorkOrder
        {
            Creator = creator,
            Number = "OD2",
            Title = "New",
            Status = WorkOrderStatus.Assigned,
            DueDate = new DateOnly(2020, 6, 1)
        };
        var bus = new StubOverdueBus([newer, older]);

        var result = await WorkOrderTools.ListOverdueWorkOrders(bus);

        result.ShouldContain("OD1");
        result.ShouldContain("OD2");
        result.IndexOf("OD1", StringComparison.Ordinal)
            .ShouldBeLessThan(result.IndexOf("OD2", StringComparison.Ordinal));
    }

    private sealed class StubOverdueBus(WorkOrder[] overdueOrders) : IBus
    {
        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
        {
            if (request is WorkOrderSpecificationQuery { OverdueOnly: true })
            {
                return Task.FromResult((TResponse)(object)overdueOrders);
            }

            throw new NotImplementedException();
        }

        public Task<object?> Send(object request) => throw new NotImplementedException();

        public Task Publish(INotification notification) => throw new NotImplementedException();
    }
}
