using System.Text;
using ClearMeasure.Bootcamp.AcceptanceTests.Extensions;
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
    public async Task ShouldCreateWorkOrderWithMaxLengthInstructionsAndPersist()
    {
        await LoginAsCurrentUser();

        var instructions4000 = new StringBuilder(4000);
        for (var i = 0; i < 4000; i++)
        {
            instructions4000.Append((char)('a' + (i % 26)));
        }

        var instructionsText = instructions4000.ToString();

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        ILocator woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(woNumberLocator).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 30_000 });
        var newWorkOrderNumber = await woNumberLocator.InnerTextAsync();

        await Input(nameof(WorkOrderManage.Elements.Title), $"[{TestTag}] instructions max");
        await Input(nameof(WorkOrderManage.Elements.Description), "Description for instructions test");
        await Input(nameof(WorkOrderManage.Elements.Instructions), instructionsText);
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), "101");

        var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
        await Click(saveButtonTestId);
        await Page.WaitForURLAsync("**/workorder/search", new PageWaitForURLOptions { Timeout = 90_000 });
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        WorkOrder? rehydrated = null;
        for (var attempt = 0; attempt < 10; attempt++)
        {
            rehydrated = await Bus.Send(new WorkOrderByNumberQuery(newWorkOrderNumber));
            if (rehydrated != null) break;
            await Task.Delay(1000);
        }
        rehydrated.ShouldNotBeNull();
        rehydrated!.Instructions!.Length.ShouldBe(4000);
        rehydrated.Instructions.ShouldBe(instructionsText);
    }

    [Test, Retry(2)]
    public async Task ShouldCreateWorkOrderWithEmptyInstructions()
    {
        await LoginAsCurrentUser();

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        ILocator woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(woNumberLocator).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 30_000 });
        var newWorkOrderNumber = await woNumberLocator.InnerTextAsync();

        await Input(nameof(WorkOrderManage.Elements.Title), $"[{TestTag}] no instructions");
        await Input(nameof(WorkOrderManage.Elements.Description), "Desc only");
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), "202");

        var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
        await Click(saveButtonTestId);
        await Page.WaitForURLAsync("**/workorder/search", new PageWaitForURLOptions { Timeout = 90_000 });
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        WorkOrder? rehydrated = null;
        for (var attempt = 0; attempt < 10; attempt++)
        {
            rehydrated = await Bus.Send(new WorkOrderByNumberQuery(newWorkOrderNumber));
            if (rehydrated != null) break;
            await Task.Delay(1000);
        }
        rehydrated.ShouldNotBeNull();
        string.IsNullOrEmpty(rehydrated!.Instructions).ShouldBeTrue();
    }

    [Test, Retry(2)]
    public async Task ShouldPersistInstructionsAfterSaveAssignAndReturn()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkOrder();

        await Page.WaitForURLAsync("**/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var workOrderLink = Page.GetByTestId(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await workOrderLink.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 30_000 });
        await ClickWorkOrderNumberFromSearchPage(order);

        await Input(nameof(WorkOrderManage.Elements.Instructions), "Bring ladder from storage.");
        await Select(nameof(WorkOrderManage.Elements.Assignee), CurrentUser.UserName);
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + DraftToAssignedCommand.Name);

        await Page.WaitForURLAsync("**/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await workOrderLink.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 30_000 });
        await ClickWorkOrderNumberFromSearchPage(order);

        var instructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions));
        await Expect(instructionsField).ToHaveValueAsync("Bring ladder from storage.");

        WorkOrder rehydrated = await Bus.Send(new WorkOrderByNumberQuery(order.Number!)) ?? throw new InvalidOperationException();
        rehydrated.Instructions.ShouldBe("Bring ladder from storage.");
    }
}
