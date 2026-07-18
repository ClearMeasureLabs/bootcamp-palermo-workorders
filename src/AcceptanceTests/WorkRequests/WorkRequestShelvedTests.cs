using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkRequests;

public class WorkRequestShelvedTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task ShouldShelveInProgressWorkRequest()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkRequest();
        order = await ClickWorkRequestNumberFromSearchPage(order);
        order = await AssignExistingWorkRequest(order, CurrentUser.UserName);
        order = await ClickWorkRequestNumberFromSearchPage(order);
        order = await BeginExistingWorkRequest(order);
        order = await ClickWorkRequestNumberFromSearchPage(order);

        order.Title = "Title from automation";
        order.Description = "Description";
        await Input(nameof(WorkRequestManage.Elements.Title), order.Title);
        await Input(nameof(WorkRequestManage.Elements.Description), order.Description);
        await Click(nameof(WorkRequestManage.Elements.CommandButton) + InProgressToAssignedCommand.Name);
        order = await ClickWorkRequestNumberFromSearchPage(order);

        await Expect(Page.GetByTestId(nameof(WorkRequestManage.Elements.Title))).ToHaveValueAsync(order.Title!);
        await Expect(Page.GetByTestId(nameof(WorkRequestManage.Elements.Description))).ToHaveValueAsync(order.Description!);
        await Expect(Page.GetByTestId(nameof(WorkRequestManage.Elements.Assignee))).ToHaveValueAsync(CurrentUser.UserName);
        await Expect(Page.GetByTestId(nameof(WorkRequestManage.Elements.Status))).ToHaveTextAsync(WorkRequestStatus.Assigned.FriendlyName);
    }
}