using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using Shouldly;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

[TestFixture]
public class WorkOrderDuplicateTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task ShouldDuplicateSavedWorkOrder_AndSaveTheCopy()
    {
        await LoginAsCurrentUser();

        await Page.GotoAsync("/workorder/manage?mode=New");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var originalNumber = await Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber))
            .InnerTextAsync();

        await Input(nameof(WorkOrderManage.Elements.Title), "Dup Source Title");
        await Input(nameof(WorkOrderManage.Elements.Description), "Dup Source Description");
        await Input(nameof(WorkOrderManage.Elements.Instructions), "Dup Source Instructions");
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), "42");

        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);
        await Page.WaitForURLAsync("**/workorder/search**");

        await Page.GotoAsync($"/workorder/manage/{originalNumber}?Mode=Edit");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber)))
            .ToHaveTextAsync(originalNumber);
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Title)))
            .ToHaveValueAsync("Dup Source Title");

        await Click(nameof(WorkOrderManage.Elements.Duplicate));

        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Title)))
            .ToHaveValueAsync("Copy of Dup Source Title");
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Description)))
            .ToHaveValueAsync("Dup Source Description");
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions)))
            .ToHaveValueAsync("Dup Source Instructions");
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.RoomNumber)))
            .ToHaveValueAsync("42");
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Status)))
            .ToHaveTextAsync("Draft");

        var newNumber = await Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber))
            .InnerTextAsync();
        newNumber.ShouldNotBe(originalNumber);

        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);
        await Page.WaitForURLAsync("**/workorder/search**");

        await Page.GotoAsync($"/workorder/manage/{newNumber}?Mode=Edit");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Title)))
            .ToHaveValueAsync("Copy of Dup Source Title");
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Description)))
            .ToHaveValueAsync("Dup Source Description");
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.RoomNumber)))
            .ToHaveValueAsync("42");
    }
}
