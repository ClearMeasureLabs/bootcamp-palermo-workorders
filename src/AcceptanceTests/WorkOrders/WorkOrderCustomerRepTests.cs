using System.Text.RegularExpressions;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderCustomerRepTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task ShouldSaveWorkOrderWith50CharacterCustomerRep()
    {
        await LoginAsCurrentUser();

        var customerRep = new string('C', WorkOrder.CustomerRepMaxLength);
        var order = Faker<WorkOrder>();
        order.Title = $"[{TestTag}] 50 char customer rep";
        order.Number = null;
        order.CustomerRep = customerRep;

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(woNumberLocator).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 30_000 });
        order.Number = await woNumberLocator.InnerTextAsync();

        await Input(nameof(WorkOrderManage.Elements.Title), order.Title);
        await Input(nameof(WorkOrderManage.Elements.Description), order.Description);
        await Input(nameof(WorkOrderManage.Elements.CustomerRep), customerRep);

        var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
        await Click(saveButtonTestId);
        await Page.WaitForURLAsync("**/workorder/search", new PageWaitForURLOptions { Timeout = 90_000 });

        var workOrderLink = Page.GetByTestId(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await workOrderLink.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 30_000 });
        await ClickWorkOrderNumberFromSearchPage(order);

        var customerRepField = Page.GetByTestId(nameof(WorkOrderManage.Elements.CustomerRep));
        await Expect(customerRepField).ToHaveValueAsync(customerRep);

        WorkOrder rehydratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number!))
            ?? throw new InvalidOperationException();
        rehydratedOrder.CustomerRep.ShouldBe(customerRep);
        rehydratedOrder.CustomerRep!.Length.ShouldBe(WorkOrder.CustomerRepMaxLength);
    }

    [Test, Retry(2)]
    public async Task ShouldRejectCustomerRepLongerThan50Characters()
    {
        await LoginAsCurrentUser();

        var tooLong = new string('X', WorkOrder.CustomerRepMaxLength + 1);

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(woNumberLocator).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 30_000 });
        var number = await woNumberLocator.InnerTextAsync();

        await Input(nameof(WorkOrderManage.Elements.Title), $"[{TestTag}] 51 char customer rep");
        await Input(nameof(WorkOrderManage.Elements.Description), "description");

        var customerRepField = Page.GetByTestId(nameof(WorkOrderManage.Elements.CustomerRep));
        await Expect(customerRepField).ToBeEditableAsync(new LocatorAssertionsToBeEditableOptions { Timeout = 30_000 });
        await customerRepField.EvaluateAsync("el => el.removeAttribute('maxlength')");
        await customerRepField.FillAsync(tooLong);
        await customerRepField.BlurAsync();

        var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
        await Click(saveButtonTestId);

        await Expect(Page).ToHaveURLAsync(new Regex("workorder/manage"));
        await Expect(Page.GetByText("Customer Rep cannot exceed 50 characters.")).ToBeVisibleAsync(
            new LocatorAssertionsToBeVisibleOptions { Timeout = 15_000 });

        WorkOrder? stored = await Bus.Send(new WorkOrderByNumberQuery(number));
        stored.ShouldBeNull();
    }
}
