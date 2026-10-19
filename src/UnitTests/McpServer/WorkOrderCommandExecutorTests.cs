using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Services;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.McpServer;

[TestFixture]
public class WorkOrderCommandExecutorTests
{
    [Test]
    public void FormatInvalid_WhenStatusMatchesButUserCannotRun_ReturnsNotAllowedMessage()
    {
        var order = new WorkOrder
        {
            Number = "123",
            Status = WorkOrderStatus.Assigned
        };

        var message = StateCommandValidationMessages.FormatInvalid(
            "AssignedToInProgressCommand",
            order,
            WorkOrderStatus.Assigned);

        message.ShouldBe(
            "Command 'AssignedToInProgressCommand' cannot be executed. You are not allowed to run this command on this work order.");
    }

    [Test]
    public void FormatInvalid_WhenStatusMismatches_ReturnsStatusMessage()
    {
        var order = new WorkOrder
        {
            Number = "123",
            Status = WorkOrderStatus.Draft
        };

        var message = StateCommandValidationMessages.FormatInvalid(
            "AssignedToInProgressCommand",
            order,
            WorkOrderStatus.Assigned);

        message.ShouldBe(
            $"Command 'AssignedToInProgressCommand' cannot be executed. Work order is in '{WorkOrderStatus.Draft.FriendlyName}' status but the command requires '{WorkOrderStatus.Assigned.FriendlyName}' status.");
    }
}
