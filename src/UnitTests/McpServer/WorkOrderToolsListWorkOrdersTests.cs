using ClearMeasure.Bootcamp.McpServer.Tools;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.McpServer;

[TestFixture]
public class WorkOrderToolsListWorkOrdersTests
{
    [TestCase("closed")]
    [TestCase("done")]
    public async Task ListWorkOrders_UnknownStatus_ReturnsValidStatusesIncludingCancelled(string status)
    {
        var result = await WorkOrderTools.ListWorkOrders(null!, status);

        result.ShouldContain($"Unknown status '{status}'");
        result.ShouldContain("Valid statuses:");
        result.ShouldContain("Draft");
        result.ShouldContain("Assigned");
        result.ShouldContain("InProgress");
        result.ShouldContain("Complete");
        result.ShouldContain("Cancelled");
    }
}
