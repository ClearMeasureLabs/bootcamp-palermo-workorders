using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using ClearMeasure.Bootcamp.UI.Shared;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class WorkOrdersExportControllerTests
{
    [Test]
    public async Task Get_Should_ReturnCsvContent_When_OrdersMatch()
    {
        var order = new WorkOrder
        {
            Number = "WO-9",
            Title = "Paint",
            Status = WorkOrderStatus.Assigned,
            Creator = new Employee("carol", "Carol", "C", "c@test.com"),
            RoomNumber = "200"
        };
        var controller = new WorkOrdersExportController(new StubExportBus([order]));

        var result = await controller.Get();

        var content = result.ShouldBeOfType<ContentResult>();
        content.ContentType.ShouldBe("text/csv; charset=utf-8");
        content.Content.ShouldNotBeNull();
        content.Content!.ShouldContain("WO-9");
        content.Content.ShouldContain("Paint");
        content.Content.ShouldContain("Assigned");
        content.Content.ShouldContain("carol");
    }

    [Test]
    public async Task Get_Should_ApplyFilters_When_QueryParametersProvided()
    {
        var bus = new StubExportBus([]);
        var controller = new WorkOrdersExportController(bus);

        await controller.Get(status: WorkOrderStatus.Draft.Key, creator: "alice", assignee: "bob");

        bus.LastQuery.ShouldNotBeNull();
        bus.LastQuery!.StatusKey.ShouldBe(WorkOrderStatus.Draft.Key);
        bus.LastQuery.Creator!.UserName.ShouldBe("alice");
        bus.LastQuery.Assignee!.UserName.ShouldBe("bob");
    }

    private sealed class StubExportBus(WorkOrder[] results) : Bus(null!)
    {
        public WorkOrderSpecificationQuery? LastQuery { get; private set; }

        public override Task Publish(INotification notification)
        {
            return Task.CompletedTask;
        }

        public override Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
        {
            if (request is WorkOrderSpecificationQuery query)
            {
                LastQuery = query;
                return Task.FromResult((TResponse)(object)results);
            }

            throw new NotImplementedException();
        }
    }
}
