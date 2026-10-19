using ClearMeasure.Bootcamp.Core.Model;
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

        var order = await CreateAndSaveNewWorkOrder();
        await ClickWorkOrderNumberFromSearchPage(order);

        var duplicateButton = Page.GetByTestId(nameof(WorkOrderManage.Elements.Duplicate));
        await duplicateButton.WaitForAsync(new() { Timeout = 60000 });
        await Expect(duplicateButton).ToBeVisibleAsync();
        await duplicateButton.ClickAsync();

        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Title)))
            .ToHaveValueAsync("Copy of " + order.Title);
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Description)))
            .ToHaveValueAsync(order.Description!);
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions)))
            .ToHaveValueAsync(order.Instructions ?? string.Empty);
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.RoomNumber)))
            .ToHaveValueAsync(order.RoomNumber!);
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Status)))
            .ToHaveTextAsync("Draft");

        var newNumber = await Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber))
            .InnerTextAsync();
        newNumber.ShouldNotBe(order.Number);

        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);
        await Page.WaitForURLAsync("**/workorder/search", new() { Timeout = 90_000 });

        var copy = new WorkOrder { Number = newNumber, Title = "Copy of " + order.Title };
        await ClickWorkOrderNumberFromSearchPage(copy);

        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Title)))
            .ToHaveValueAsync("Copy of " + order.Title);
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Description)))
            .ToHaveValueAsync(order.Description!);
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.RoomNumber)))
            .ToHaveValueAsync(order.RoomNumber!);
    }
}
