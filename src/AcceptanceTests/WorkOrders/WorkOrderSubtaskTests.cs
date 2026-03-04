using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderSubtaskTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task ShouldAddSubtaskAndVerifyItAppearsInChecklist()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkOrder();
        await ClickWorkOrderNumberFromSearchPage(order);

        var subtaskInput = Page.GetByTestId(nameof(WorkOrderManage.Elements.SubtaskNewTitle));
        await Expect(subtaskInput).ToBeVisibleAsync();
        await subtaskInput.FillAsync("Replace light fixture");

        await Click(nameof(WorkOrderManage.Elements.SubtaskAddButton));
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var checklist = Page.GetByTestId(nameof(WorkOrderManage.Elements.SubtaskChecklist));
        await Expect(checklist).ToContainTextAsync("Replace light fixture");
    }

    [Test, Retry(2)]
    public async Task ShouldToggleSubtaskCompletionAndVerifyVisualState()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkOrder();
        await ClickWorkOrderNumberFromSearchPage(order);

        var subtaskInput = Page.GetByTestId(nameof(WorkOrderManage.Elements.SubtaskNewTitle));
        await subtaskInput.FillAsync("Fix wiring");
        await Click(nameof(WorkOrderManage.Elements.SubtaskAddButton));
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var checkboxes = Page.Locator($"[data-testid^='{nameof(WorkOrderManage.Elements.SubtaskToggle)}']");
        await checkboxes.First.WaitForAsync();
        await checkboxes.First.CheckAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Expect(checkboxes.First).ToBeCheckedAsync();
    }

    [Test, Retry(2)]
    public async Task ShouldShowCorrectProgressAfterAddingAndCompletingSubtasks()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkOrder();
        await ClickWorkOrderNumberFromSearchPage(order);

        foreach (var title in new[] { "Task A", "Task B", "Task C" })
        {
            var input = Page.GetByTestId(nameof(WorkOrderManage.Elements.SubtaskNewTitle));
            await input.FillAsync(title);
            await Click(nameof(WorkOrderManage.Elements.SubtaskAddButton));
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }

        var checkboxes = Page.Locator($"[data-testid^='{nameof(WorkOrderManage.Elements.SubtaskToggle)}']");
        await checkboxes.Nth(0).CheckAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await checkboxes.Nth(1).CheckAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var progress = Page.GetByTestId(nameof(WorkOrderManage.Elements.SubtaskProgress));
        await Expect(progress).ToContainTextAsync("2 of 3 complete");
    }
}
