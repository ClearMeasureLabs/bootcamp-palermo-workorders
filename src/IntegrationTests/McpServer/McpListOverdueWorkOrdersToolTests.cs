using System.Text.Json;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.IntegrationTests.DataAccess;
using ClearMeasure.Bootcamp.McpServer.Tools;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.McpServer;

[TestFixture]
public class McpListOverdueWorkOrdersToolTests
{
    [SetUp]
    public void Setup()
    {
        new DatabaseTests().Clean();
    }

    [Test]
    public async Task ShouldListOverdueOpenWorkOrdersOrderedByDueDate()
    {
        var employee = new Employee("oduser", "Over", "Due", "od@test.com");
        var olderOverdue = new WorkOrder
        {
            Creator = employee,
            Number = "OD-OLD",
            Title = "Older overdue",
            Status = WorkOrderStatus.Assigned,
            DueDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-5))
        };
        var newerOverdue = new WorkOrder
        {
            Creator = employee,
            Number = "OD-NEW",
            Title = "Newer overdue",
            Status = WorkOrderStatus.Draft,
            DueDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-1))
        };
        var futureDue = new WorkOrder
        {
            Creator = employee,
            Number = "OD-FUT",
            Title = "Future due",
            Status = WorkOrderStatus.Draft,
            DueDate = DateOnly.FromDateTime(DateTime.Today.AddDays(3))
        };
        var completedOverdue = new WorkOrder
        {
            Creator = employee,
            Number = "OD-CMP",
            Title = "Completed overdue",
            Status = WorkOrderStatus.Complete,
            DueDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-2))
        };
        var cancelledOverdue = new WorkOrder
        {
            Creator = employee,
            Number = "OD-CAN",
            Title = "Cancelled overdue",
            Status = WorkOrderStatus.Cancelled,
            DueDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-2))
        };

        await using var context = TestHost.GetRequiredService<DbContext>();
        context.Add(employee);
        context.Add(olderOverdue);
        context.Add(newerOverdue);
        context.Add(futureDue);
        context.Add(completedOverdue);
        context.Add(cancelledOverdue);
        await context.SaveChangesAsync();

        var bus = TestHost.GetRequiredService<IBus>();
        var result = await WorkOrderTools.ListOverdueWorkOrders(bus);

        result.ShouldContain("OD-OLD");
        result.ShouldContain("OD-NEW");
        result.ShouldNotContain("OD-FUT");
        result.ShouldNotContain("OD-CMP");
        result.ShouldNotContain("OD-CAN");

        using var doc = JsonDocument.Parse(result);
        var numbers = doc.RootElement.EnumerateArray()
            .Select(e => e.GetProperty("Number").GetString())
            .ToArray();
        numbers.ShouldBe(["OD-OLD", "OD-NEW"]);
    }
}
