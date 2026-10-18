using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.McpServer.Tools;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.McpServer;

[TestFixture]
public class DatedWorkOrderSchedulingFormatResultTests
{
    [Test]
    public void FormatResult_ListsWorkOrdersByDueDateAscending()
    {
        var result = new CreateDatedWorkOrdersResult(
            Success: true,
            Message: string.Empty,
            WorkOrders:
            [
                new CreatedDatedWorkOrder("WO-001", new DateOnly(2026, 11, 14)),
                new CreatedDatedWorkOrder("WO-002", new DateOnly(2026, 11, 7))
            ]);

        var formatted = DatedWorkOrderScheduling.FormatResult(result);

        var earlier = formatted.IndexOf("2026-11-07", StringComparison.Ordinal);
        var later = formatted.IndexOf("2026-11-14", StringComparison.Ordinal);
        earlier.ShouldBeGreaterThan(-1);
        later.ShouldBeGreaterThan(-1);
        earlier.ShouldBeLessThan(later);
    }
}
