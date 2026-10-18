using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.McpServer.Tools;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace ClearMeasure.Bootcamp.AcceptanceTests.McpServer;

[TestFixture]
public class ListOverdueWorkOrdersMcpTests
{
    [Test]
    public async Task ShouldReturnOverdueWorkOrdersFromMcpTool()
    {
        var employee = new Employee("mcpod", "Mcp", "Overdue", "mcpod@test.com");
        var overdue = new WorkOrder
        {
            Creator = employee,
            Number = "MCP-OD1",
            Title = "Overdue via MCP",
            Status = WorkOrderStatus.Assigned,
            DueDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-4))
        };

        await using var context = TestHost.GetRequiredService<DbContext>();
        context.Add(employee);
        context.Add(overdue);
        await context.SaveChangesAsync();

        var bus = TestHost.GetRequiredService<IBus>();
        var result = await WorkOrderTools.ListOverdueWorkOrders(bus);

        result.ShouldContain("MCP-OD1");
        result.ShouldContain("Overdue via MCP");
    }
}
