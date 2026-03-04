using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderTemplateTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task ShouldCreateTemplateAndVerifyInTemplateList()
    {
        await LoginAsCurrentUser();

        await Click(nameof(NavMenu.Elements.Templates));
        await Page.WaitForURLAsync("**/workorder/templates");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var templateTitle = $"[{TestTag}] Weekly Bathroom Cleaning";
        var templateDescription = "Clean all bathrooms on floor 1";
        var templateRoomNumber = "B101";

        await Input(nameof(WorkOrderTemplates.Elements.TemplateTitle), templateTitle);
        await Input(nameof(WorkOrderTemplates.Elements.TemplateDescription), templateDescription);
        await Input(nameof(WorkOrderTemplates.Elements.TemplateRoomNumber), templateRoomNumber);
        await Click(nameof(WorkOrderTemplates.Elements.SaveTemplateButton));

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Expect(Page.GetByTestId(nameof(WorkOrderTemplates.Elements.TemplateRowTitle) + templateTitle))
            .ToBeVisibleAsync();
    }

    [Test, Retry(2)]
    public async Task ShouldPopulateFormFieldsFromTemplate()
    {
        await LoginAsCurrentUser();

        var templateTitle = $"[{TestTag}] Weekly Bathroom Cleaning";
        var templateDescription = "Clean all bathrooms on floor 1";
        var templateRoomNumber = "B101";

        await Bus.Send(new CreateWorkOrderTemplateCommand(
            templateTitle,
            templateDescription,
            templateRoomNumber,
            CurrentUser.Id));

        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var templateSelectLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.TemplateSelect));
        await Expect(templateSelectLocator).ToBeVisibleAsync();

        await templateSelectLocator.SelectOptionAsync(new SelectOptionValue { Label = templateTitle });
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Title)))
            .ToHaveValueAsync(templateTitle, new LocatorAssertionsToHaveValueOptions { Timeout = 10_000 });
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Description)))
            .ToHaveValueAsync(templateDescription, new LocatorAssertionsToHaveValueOptions { Timeout = 10_000 });
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.RoomNumber)))
            .ToHaveValueAsync(templateRoomNumber, new LocatorAssertionsToHaveValueOptions { Timeout = 10_000 });
    }

    [Test, Retry(2)]
    public async Task ShouldSaveTemplatedWorkOrderAsDraft()
    {
        await LoginAsCurrentUser();

        var templateTitle = $"[{TestTag}] Weekly Bathroom Cleaning";
        var templateDescription = "Clean all bathrooms on floor 1";
        var templateRoomNumber = "B101";

        await Bus.Send(new CreateWorkOrderTemplateCommand(
            templateTitle,
            templateDescription,
            templateRoomNumber,
            CurrentUser.Id));

        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(woNumberLocator).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 30_000 });
        var workOrderNumber = (await woNumberLocator.InnerTextAsync()).Trim();

        var templateSelectLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.TemplateSelect));
        await Expect(templateSelectLocator).ToBeVisibleAsync();

        await templateSelectLocator.SelectOptionAsync(new SelectOptionValue { Label = templateTitle });
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Title)))
            .ToHaveValueAsync(templateTitle, new LocatorAssertionsToHaveValueOptions { Timeout = 10_000 });

        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        WorkOrder? savedOrder = null;
        for (var attempt = 0; attempt < 10; attempt++)
        {
            savedOrder = await Bus.Send(new WorkOrderByNumberQuery(workOrderNumber));
            if (savedOrder != null) break;
            await Task.Delay(1000);
        }

        savedOrder.ShouldNotBeNull();
        savedOrder.Status.ShouldBe(WorkOrderStatus.Draft);
        savedOrder.Title.ShouldBe(templateTitle);
        savedOrder.Description.ShouldBe(templateDescription);
        savedOrder.RoomNumber.ShouldBe(templateRoomNumber);
    }
}
