using ClearMeasure.Bootcamp.AcceptanceTests.Extensions;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using System.Globalization;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkRequests;

public class WorkRequestAssignTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task ShouldAssignEmployeeAndAssign()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkRequest();

        await Click(nameof(WorkRequestSearch.Elements.WorkRequestLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var woNumberLocator = Page.GetByTestId(nameof(WorkRequestManage.Elements.WorkRequestNumber));
        await woNumberLocator.WaitForAsync();
        await Expect(woNumberLocator).ToHaveTextAsync(order.Number!);
        
        await Select(nameof(WorkRequestManage.Elements.Assignee), CurrentUser.UserName);
        await Input(nameof(WorkRequestManage.Elements.Title), "newtitle");
        await Input(nameof(WorkRequestManage.Elements.Description), "newdesc");
        await Click(nameof(WorkRequestManage.Elements.CommandButton) + DraftToAssignedCommand.Name);

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(WorkRequestSearch.Elements.WorkRequestLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await woNumberLocator.WaitForAsync();
        await Expect(woNumberLocator).ToHaveTextAsync(order.Number!);

        var titleField = Page.GetByTestId(nameof(WorkRequestManage.Elements.Title));
        await Expect(titleField).ToHaveValueAsync("newtitle");
        
        var descriptionField = Page.GetByTestId(nameof(WorkRequestManage.Elements.Description));
        await Expect(descriptionField).ToHaveValueAsync("newdesc");
        
        var assigneeField = Page.GetByTestId(nameof(WorkRequestManage.Elements.Assignee));
        await Expect(assigneeField).ToBeDisabledAsync();
        await Expect(assigneeField).ToHaveValueAsync(CurrentUser.UserName);

        WorkRequest rehyratedOrder = await Bus.Send(new WorkRequestByNumberQuery(order.Number!)) ?? throw new InvalidOperationException();
        var displayedDate = await Page.GetDateTimeFromTestIdAsync(nameof(WorkRequestManage.Elements.AssignedDate));

        rehyratedOrder.AssignedDate.TruncateToMinute().ShouldBe(displayedDate);
    }
}