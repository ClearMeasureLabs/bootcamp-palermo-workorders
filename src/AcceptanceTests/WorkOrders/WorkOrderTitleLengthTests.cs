using System.Text.RegularExpressions;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderTitleLengthTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task ShouldSaveWorkOrderWith320CharacterTitle()
    {
        await LoginAsCurrentUser();

        var title = new string('T', WorkOrder.TitleMaxLength);
        var order = Faker<WorkOrder>();
        order.Title = title;
        order.Number = null;

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(woNumberLocator).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 30_000 });
        order.Number = await woNumberLocator.InnerTextAsync();

        await Input(nameof(WorkOrderManage.Elements.Title), title);
        await Input(nameof(WorkOrderManage.Elements.Description), order.Description);

        var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
        await Click(saveButtonTestId);
        await Page.WaitForURLAsync("**/workorder/search", new PageWaitForURLOptions { Timeout = 90_000 });

        var workOrderLink = Page.GetByTestId(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await workOrderLink.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 30_000 });
        await ClickWorkOrderNumberFromSearchPage(order);

        var titleField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Title));
        await Expect(titleField).ToHaveValueAsync(title);

        WorkOrder rehydratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number!))
            ?? throw new InvalidOperationException();
        rehydratedOrder.Title.ShouldBe(title);
        rehydratedOrder.Title!.Length.ShouldBe(WorkOrder.TitleMaxLength);
    }

    [Test, Retry(2)]
    public async Task ShouldRejectTitleLongerThan320Characters()
    {
        await LoginAsCurrentUser();

        var tooLong = new string('X', WorkOrder.TitleMaxLength + 1);

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(woNumberLocator).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 30_000 });
        var number = await woNumberLocator.InnerTextAsync();

        await Input(nameof(WorkOrderManage.Elements.Description), "description");

        var titleField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Title));
        await Expect(titleField).ToBeEditableAsync(new LocatorAssertionsToBeEditableOptions { Timeout = 30_000 });
        await titleField.EvaluateAsync("el => el.removeAttribute('maxlength')");
        await titleField.FillAsync(tooLong);
        await titleField.BlurAsync();

        var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
        await Click(saveButtonTestId);

        await Expect(Page).ToHaveURLAsync(new Regex("workorder/manage"));
        await Expect(Page.GetByText("Title cannot exceed 320 characters.")).ToBeVisibleAsync(
            new LocatorAssertionsToBeVisibleOptions { Timeout = 15_000 });

        WorkOrder? stored = await Bus.Send(new WorkOrderByNumberQuery(number));
        stored.ShouldBeNull();
    }
}
