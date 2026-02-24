using ClearMeasure.Bootcamp.McpServer.Resources;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.McpServer;

[TestFixture]
public class McpReferenceResourceTests
{
    [Test]
    public void ShouldReturnAllWorkOrderStatuses()
    {
        var result = ReferenceResources.GetWorkOrderStatuses();

        result.ShouldContain("Draft");
        result.ShouldContain("Assigned");
        result.ShouldContain("InProgress");
        result.ShouldContain("Complete");
        result.ShouldContain("Cancelled");
        result.ShouldContain("DFT");
        result.ShouldContain("ASD");
        result.ShouldContain("IPG");
        result.ShouldContain("CMP");
        result.ShouldContain("CNL");
    }

    [Test]
    public void ShouldReturnAllRoles()
    {
        var result = ReferenceResources.GetRoles();

        result.ShouldContain("Manager");
        result.ShouldContain("Worker");
        result.ShouldContain("Admin");
        result.ShouldContain("CanCreateWorkOrder");
        result.ShouldContain("CanFulfillWorkOrder");
    }

    [Test]
    public void ShouldReturnStatusTransitions()
    {
        var result = ReferenceResources.GetStatusTransitions();

        result.ShouldContain("DraftToAssignedCommand");
        result.ShouldContain("AssignedToInProgressCommand");
        result.ShouldContain("InProgressToCompleteCommand");
        result.ShouldContain("AssignedToCancelledCommand");
        result.ShouldContain("InProgressToCancelledCommand");
        result.ShouldContain("InProgressToAssigned");
    }
}
