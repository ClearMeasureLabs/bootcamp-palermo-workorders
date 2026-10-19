using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.IntegrationTests.DataAccess;
using ClearMeasure.Bootcamp.McpServer.Tools;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.McpServer;

[TestFixture]
public class McpListRecentWorkOrdersTests
{
    [SetUp]
    public void Setup()
    {
        new DatabaseTests().Clean();
    }

    [Test]
    public async Task ShouldListRecentWorkOrders_UsingSameSummaryShapeAsListWorkOrders()
    {
        var employee = new Employee("user1", "John", "Doe", "john@test.com");
        var now = DateTime.UtcNow;
        var recent = new WorkOrder
        {
            Creator = employee,
            Number = "WO-R01",
            Title = "Recent order",
            CreatedDate = now.AddDays(-1)
        };
        var old = new WorkOrder
        {
            Creator = employee,
            Number = "WO-R02",
            Title = "Old order",
            CreatedDate = now.AddDays(-20)
        };

        await using var context = TestHost.GetRequiredService<DbContext>();
        context.Add(employee);
        context.Add(recent);
        context.Add(old);
        await context.SaveChangesAsync();

        var bus = TestHost.GetRequiredService<IBus>();
        var result = await WorkOrderTools.ListRecentWorkOrders(bus, days: 7);

        result.ShouldContain("WO-R01");
        result.ShouldContain("Recent order");
        result.ShouldNotContain("WO-R02");
        result.ShouldNotContain("Old order");
        result.ShouldContain("Status");
        result.ShouldContain("Creator");
    }

    [Test]
    public async Task ShouldDefaultDaysToSeven()
    {
        var employee = new Employee("user1", "John", "Doe", "john@test.com");
        var now = DateTime.UtcNow;
        var withinDefault = new WorkOrder
        {
            Creator = employee,
            Number = "WO-R03",
            Title = "Within default",
            CreatedDate = now.AddDays(-3)
        };
        var outsideDefault = new WorkOrder
        {
            Creator = employee,
            Number = "WO-R04",
            Title = "Outside default",
            CreatedDate = now.AddDays(-14)
        };

        await using var context = TestHost.GetRequiredService<DbContext>();
        context.Add(employee);
        context.Add(withinDefault);
        context.Add(outsideDefault);
        await context.SaveChangesAsync();

        var bus = TestHost.GetRequiredService<IBus>();
        var result = await WorkOrderTools.ListRecentWorkOrders(bus);

        result.ShouldContain("WO-R03");
        result.ShouldNotContain("WO-R04");
    }
}
