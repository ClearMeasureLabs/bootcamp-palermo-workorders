using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderInstructionTemplateTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task ShouldInsertInstructionTemplateSaveDraftAndSeeTextOnReopen()
    {
        await LoginAsCurrentUser();

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");
        await WaitForNewWorkOrderFormReadyAsync();

        var order = new WorkOrder();
        var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        order.Number = await woNumberLocator.InnerTextAsync();
        var template = WorkOrderManage.InstructionTemplates[0];

        await Input(nameof(WorkOrderManage.Elements.Title), $"[{TestTag}] instruction template");
        await Input(nameof(WorkOrderManage.Elements.Description), "Template insertion acceptance");
        await Select(nameof(WorkOrderManage.Elements.InstructionTemplate), template);

        var instructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions));
        await Expect(instructionsField).ToHaveValueAsync(template);

        var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
        await Click(saveButtonTestId);
        await Page.WaitForURLAsync("**/workorder/search", new PageWaitForURLOptions { Timeout = 90_000 });

        await ClickWorkOrderNumberFromSearchPage(order);

        instructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions));
        await Expect(instructionsField).ToHaveValueAsync(template);
    }
}
