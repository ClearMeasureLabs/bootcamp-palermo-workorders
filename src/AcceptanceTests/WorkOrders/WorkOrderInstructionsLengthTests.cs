using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using Shouldly;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderInstructionsLengthTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task ShouldSaveWorkOrderWith4000CharacterInstructions()
    {
        await LoginAsCurrentUser();

        var instructions = new string('I', WorkOrder.InstructionsMaxLength);
        var order = Faker<WorkOrder>();
        order.Title = $"[{TestTag}] 4000 char instructions";
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
        await Input(nameof(WorkOrderManage.Elements.Instructions), instructions);
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), order.RoomNumber);

        var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
        await Click(saveButtonTestId);
        await Page.WaitForURLAsync("**/workorder/search", new PageWaitForURLOptions { Timeout = 90_000 });

        var workOrderLink = Page.GetByTestId(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await workOrderLink.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 30_000 });
        await ClickWorkOrderNumberFromSearchPage(order);

        var instructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions));
        await Expect(instructionsField).ToHaveValueAsync(instructions);

        WorkOrder rehydratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number!))
            ?? throw new InvalidOperationException();
        rehydratedOrder.Instructions.ShouldBe(instructions);
        rehydratedOrder.Instructions!.Length.ShouldBe(WorkOrder.InstructionsMaxLength);
    }
}
