using System.Text.RegularExpressions;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using Microsoft.Playwright;
using Shouldly;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderInstructionsTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task ShouldPersistWorkOrderWith4000CharacterInstructions()
    {
        await LoginAsCurrentUser();

        var instructions = new string('i', 4000);
        var order = await CreateAndSaveNewWorkOrderWithInstructionsAsync(instructions);

        await Page.WaitForURLAsync("**/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        order.Number.ShouldNotBeNullOrWhiteSpace();
        var workOrderLink = Page.GetByTestId(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await workOrderLink.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 30_000 });

        await ClickWorkOrderNumberFromSearchPage(order);
        await Expect(Page).ToHaveURLAsync(new Regex($"/workorder/manage/{Regex.Escape(order.Number!)}\\?mode=Edit"));

        var instructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions));
        await Expect(instructionsField).ToHaveValueAsync(instructions);

        var rehydrated = await Bus.Send(new WorkOrderByNumberQuery(order.Number!)) ?? throw new InvalidOperationException();
        rehydrated.Instructions.ShouldBe(instructions);
    }

    [Test, Retry(2)]
    public async Task ShouldPersistWorkOrderWithEmptyInstructions()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkOrderWithInstructionsAsync(null);

        await Page.WaitForURLAsync("**/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var workOrderLink = Page.GetByTestId(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await workOrderLink.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 30_000 });

        await ClickWorkOrderNumberFromSearchPage(order);

        var instructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions));
        await Expect(instructionsField).ToHaveValueAsync("");

        var rehydrated = await Bus.Send(new WorkOrderByNumberQuery(order.Number!)) ?? throw new InvalidOperationException();
        rehydrated.Instructions.ShouldBe(string.Empty);
    }

    [Test, Retry(2)]
    public async Task ShouldPersistInstructionsAfterSaveAssignAndReturn()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkOrderWithInstructionsAsync("Bring tools from shed");

        await Page.WaitForURLAsync("**/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var workOrderLink = Page.GetByTestId(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await workOrderLink.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 30_000 });

        await ClickWorkOrderNumberFromSearchPage(order);

        await Select(nameof(WorkOrderManage.Elements.Assignee), CurrentUser.UserName);
        await Input(nameof(WorkOrderManage.Elements.Title), order.Title);
        await Input(nameof(WorkOrderManage.Elements.Description), order.Description);
        await Input(nameof(WorkOrderManage.Elements.Instructions), "Updated: check valve first");
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + DraftToAssignedCommand.Name);

        await Page.WaitForURLAsync("**/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await workOrderLink.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 30_000 });
        await ClickWorkOrderNumberFromSearchPage(order);

        var instructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions));
        await Expect(instructionsField).ToHaveValueAsync("Updated: check valve first");

        var rehydrated = await Bus.Send(new WorkOrderByNumberQuery(order.Number!)) ?? throw new InvalidOperationException();
        rehydrated.Instructions.ShouldBe("Updated: check valve first");
    }

    private async Task<WorkOrder> CreateAndSaveNewWorkOrderWithInstructionsAsync(string? instructions)
    {
        var order = Faker<WorkOrder>();
        order.Title = $"[{TestTag}] instructions test";
        order.Number = null;
        order.Instructions = instructions;

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(woNumberLocator).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 30_000 });
        order.Number = await woNumberLocator.InnerTextAsync();

        await Input(nameof(WorkOrderManage.Elements.Title), order.Title);
        await Input(nameof(WorkOrderManage.Elements.Description), order.Description);
        await Input(nameof(WorkOrderManage.Elements.Instructions), instructions ?? "");
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), order.RoomNumber);

        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);
        await Page.WaitForURLAsync("**/workorder/search", new PageWaitForURLOptions { Timeout = 90_000 });
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        WorkOrder? rehydrated = null;
        for (var attempt = 0; attempt < 10; attempt++)
        {
            rehydrated = await Bus.Send(new WorkOrderByNumberQuery(order.Number));
            if (rehydrated != null)
            {
                break;
            }

            await Task.Delay(1000);
        }

        rehydrated.ShouldNotBeNull();
        return rehydrated;
    }
}
