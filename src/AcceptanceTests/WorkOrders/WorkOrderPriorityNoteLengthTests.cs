using System.Text.RegularExpressions;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderPriorityNoteLengthTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task ShouldSaveWorkOrderWith200CharacterPriorityNote()
    {
        await LoginAsCurrentUser();

        var note = new string('P', WorkOrder.PriorityNoteMaxLength);
        var order = Faker<WorkOrder>();
        order.Title = $"[{TestTag}] 200 char priority note";
        order.Number = null;
        order.PriorityNote = note;

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(woNumberLocator).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 30_000 });
        order.Number = await woNumberLocator.InnerTextAsync();

        await Input(nameof(WorkOrderManage.Elements.Title), order.Title);
        await Input(nameof(WorkOrderManage.Elements.Description), order.Description);
        await Input(nameof(WorkOrderManage.Elements.PriorityNote), note);

        var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
        await Click(saveButtonTestId);
        await Page.WaitForURLAsync("**/workorder/search", new PageWaitForURLOptions { Timeout = 90_000 });

        var workOrderLink = Page.GetByTestId(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await workOrderLink.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 30_000 });
        await ClickWorkOrderNumberFromSearchPage(order);

        var priorityNoteField = Page.GetByTestId(nameof(WorkOrderManage.Elements.PriorityNote));
        await Expect(priorityNoteField).ToHaveValueAsync(note);

        WorkOrder rehydratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number!))
            ?? throw new InvalidOperationException();
        rehydratedOrder.PriorityNote.ShouldBe(note);
        rehydratedOrder.PriorityNote!.Length.ShouldBe(WorkOrder.PriorityNoteMaxLength);
    }

    [Test, Retry(2)]
    public async Task ShouldRejectPriorityNoteLongerThan200Characters()
    {
        await LoginAsCurrentUser();

        var tooLong = new string('X', WorkOrder.PriorityNoteMaxLength + 1);

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(woNumberLocator).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 30_000 });
        var number = await woNumberLocator.InnerTextAsync();

        await Input(nameof(WorkOrderManage.Elements.Title), $"[{TestTag}] 201 char priority note");
        await Input(nameof(WorkOrderManage.Elements.Description), "description");

        var priorityNoteField = Page.GetByTestId(nameof(WorkOrderManage.Elements.PriorityNote));
        await Expect(priorityNoteField).ToBeEditableAsync(new LocatorAssertionsToBeEditableOptions { Timeout = 30_000 });
        await priorityNoteField.EvaluateAsync("el => el.removeAttribute('maxlength')");
        await priorityNoteField.FillAsync(tooLong);
        await priorityNoteField.BlurAsync();

        var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
        await Click(saveButtonTestId);

        await Expect(Page).ToHaveURLAsync(new Regex("workorder/manage"));
        await Expect(Page.GetByText("Priority note cannot exceed 200 characters.")).ToBeVisibleAsync(
            new LocatorAssertionsToBeVisibleOptions { Timeout = 15_000 });

        WorkOrder? stored = await Bus.Send(new WorkOrderByNumberQuery(number));
        stored.ShouldBeNull();
    }
}
