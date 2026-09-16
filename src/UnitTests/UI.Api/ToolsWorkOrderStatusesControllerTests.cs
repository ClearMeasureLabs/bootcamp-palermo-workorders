using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class ToolsWorkOrderStatusesControllerTests
{
    [Test]
    public void Get_Should_ReturnAllStatuses_WithExpectedCount()
    {
        var result = CreateController().Get();

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var statuses = ok.Value.ShouldBeOfType<WorkOrderStatusDto[]>();
        statuses.Length.ShouldBe(5);
    }

    [Test]
    public void Get_Should_ReturnStatusesInSortOrder()
    {
        var result = CreateController().Get();

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var statuses = ok.Value.ShouldBeOfType<WorkOrderStatusDto[]>();
        statuses.Select(s => s.SortBy).ToList().ShouldBe(
            statuses.Select(s => s.SortBy).OrderBy(x => x).ToList());
    }

    [Test]
    public void Get_Should_ReturnExpectedFields_ForDraftStatus()
    {
        var result = CreateController().Get();

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var statuses = ok.Value.ShouldBeOfType<WorkOrderStatusDto[]>();
        var draft = statuses.FirstOrDefault(s => s.Key == "Draft");
        draft.ShouldNotBeNull();
        draft!.Code.ShouldBe("DRT");
        draft.FriendlyName.ShouldBe("Draft");
        draft.SortBy.ShouldBe((byte)1);
    }

    [Test]
    public void Get_Should_ContainAllExpectedKeys()
    {
        var result = CreateController().Get();

        var ok = result.ShouldBeOfType<OkObjectResult>();
        var statuses = ok.Value.ShouldBeOfType<WorkOrderStatusDto[]>();
        var keys = statuses.Select(s => s.Key).ToHashSet();
        keys.ShouldContain("Draft");
        keys.ShouldContain("Assigned");
        keys.ShouldContain("InProgress");
        keys.ShouldContain("Complete");
        keys.ShouldContain("Cancelled");
    }

    private static ToolsWorkOrderStatusesController CreateController() =>
        new()
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
}
