using System.Text.RegularExpressions;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using Shouldly;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderInstructionsTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task MaintainWorkOrder_ShouldRoundTripInstructions_AfterSave()
    {
        await LoginAsCurrentUser();

        var order = Faker<WorkOrder>();
        order.Title = $"[{TestTag}] from automation";
        order.Number = null;
        order.Instructions = $"[{TestTag}] distinctive execution steps for round-trip";

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(woNumberLocator).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 30_000 });
        order.Number = await woNumberLocator.InnerTextAsync();

        await Input(nameof(WorkOrderManage.Elements.Title), order.Title);
        await Input(nameof(WorkOrderManage.Elements.Description), order.Description);
        await Input(nameof(WorkOrderManage.Elements.Instructions), order.Instructions);
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), order.RoomNumber);

        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        WorkOrder? rehydrated = null;
        for (var attempt = 0; attempt < 10; attempt++)
        {
            rehydrated = await Bus.Send(new WorkOrderByNumberQuery(order.Number!));
            if (rehydrated != null) break;
            await Task.Delay(1000);
        }
        rehydrated.ShouldNotBeNull();
        rehydrated.Instructions.ShouldBe(order.Instructions);

        var workOrderLink = Page.GetByTestId(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await workOrderLink.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 30_000 });
        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Expect(Page).ToHaveURLAsync(new Regex($"/workorder/manage/{Regex.Escape(order.Number!)}\\?mode=Edit"));
        var instructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions));
        await Expect(instructionsField).ToHaveValueAsync(order.Instructions!);
    }

    [Test, Retry(2)]
    public async Task MaintainWorkOrder_ShouldRejectInstructionsOverMaxLength_ViaValidationSummary()
    {
        await LoginAsCurrentUser();

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        await Input(nameof(WorkOrderManage.Elements.Title), $"[{TestTag}] title for validation");
        await Input(nameof(WorkOrderManage.Elements.Description), "Description for validation test");
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), "101");

        var tooLong = new string('z', 4001);
        await Input(nameof(WorkOrderManage.Elements.Instructions), tooLong);

        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);

        var summary = Page.Locator(".validation-summary");
        await Expect(summary).ToBeVisibleAsync();
        await Expect(summary).ToContainTextAsync("Instructions");
    }

    [Test, Retry(2)]
    public async Task MaintainWorkOrder_ShouldShowInstructionsReadOnly_When_NotEditable()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkOrder();
        order = await ClickWorkOrderNumberFromSearchPage(order);

        order = await AssignExistingWorkOrder(order, CurrentUser.UserName);
        order = await ClickWorkOrderNumberFromSearchPage(order);

        order = await BeginExistingWorkOrder(order);
        order = await ClickWorkOrderNumberFromSearchPage(order);

        order.Title = "Title for read-only instructions test";
        order.Description = "Description for read-only";
        order.Instructions = "Instructions visible but not editable after complete";
        order = await CompleteExistingWorkOrder(order);
        order = await ClickWorkOrderNumberFromSearchPage(order);

        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.ReadOnlyMessage))).ToBeVisibleAsync();
        var instructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions));
        await Expect(instructionsField).ToHaveValueAsync(order.Instructions!);
        await Expect(instructionsField).ToBeDisabledAsync();
    }
}
