using ClearMeasure.Bootcamp.AcceptanceTests.Extensions;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderTitleValidationTests : AcceptanceTestBase
{
    [Test]
    public async Task CreateWorkOrder_WithTitleExactly12Characters_Succeeds()
    {
        await LoginAsCurrentUser();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        ILocator woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(woNumberLocator).ToBeVisibleAsync();
        var newWorkOrderNumber = await woNumberLocator.InnerTextAsync();

        await Input(nameof(WorkOrderManage.Elements.Title), "Fix Plumbing");
        await Input(nameof(WorkOrderManage.Elements.Description), "Test description");
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), "101");

        var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
        await Click(saveButtonTestId);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Page.WaitForURLAsync("**/workorder/search");
        
        WorkOrder? savedOrder = await Bus.Send(new WorkOrderByNumberQuery(newWorkOrderNumber));
        savedOrder.ShouldNotBeNull();
        savedOrder.Title.ShouldBe("Fix Plumbing");
    }

    [Test]
    public async Task CreateWorkOrder_WithTitleLessThan12Characters_ShowsValidationError()
    {
        await LoginAsCurrentUser();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        await Input(nameof(WorkOrderManage.Elements.Title), "Fix Heater!");
        await Input(nameof(WorkOrderManage.Elements.Description), "Test description");
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), "101");

        var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
        await Click(saveButtonTestId);

        var validationMessage = Page.Locator(".validation-message").First;
        await Expect(validationMessage).ToBeVisibleAsync();
        var text = await validationMessage.InnerTextAsync();
        text.ShouldContain("12 characters");
    }

    [Test]
    public async Task CreateWorkOrder_WithTitleMoreThan12Characters_ShowsValidationError()
    {
        await LoginAsCurrentUser();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        // Try to input more than 12 characters - maxlength should prevent it
        var titleLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.Title));
        await titleLocator.FillAsync("Fix Roof Leak!");
        
        // Verify only 12 characters were actually entered due to maxlength
        var actualValue = await titleLocator.InputValueAsync();
        actualValue.Length.ShouldBe(12);
        actualValue.ShouldBe("Fix Roof Lea");
        
        await Input(nameof(WorkOrderManage.Elements.Description), "Test description");
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), "101");

        // The form should be submittable since client-side maxlength enforced 12 chars
        var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
        await Click(saveButtonTestId);
        
        // Should succeed and navigate to search page
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Page.WaitForURLAsync("**/workorder/search");
    }

    [Test]
    public async Task CreateWorkOrder_WithEmptyTitle_ShowsValidationError()
    {
        await LoginAsCurrentUser();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        await Input(nameof(WorkOrderManage.Elements.Title), "");
        await Input(nameof(WorkOrderManage.Elements.Description), "Test description");
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), "101");

        var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
        await Click(saveButtonTestId);

        var validationMessage = Page.Locator(".validation-message").First;
        await Expect(validationMessage).ToBeVisibleAsync();
        var text = await validationMessage.InnerTextAsync();
        text.ShouldContain("required");
    }

    [Test]
    public async Task EditWorkOrder_ChangeTitleToInvalidLength_ShowsValidationError()
    {
        await LoginAsCurrentUser();
        
        var order = await CreateAndSaveNewWorkOrder();

        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(woNumberLocator).ToHaveTextAsync(order.Number!);

        await Input(nameof(WorkOrderManage.Elements.Title), "ShortTtl");

        var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
        await Click(saveButtonTestId);

        var validationMessage = Page.Locator(".validation-message").First;
        await Expect(validationMessage).ToBeVisibleAsync();
        var text = await validationMessage.InnerTextAsync();
        text.ShouldContain("12 characters");
    }

    [Test]
    public async Task ClientSideValidation_PreventsFormSubmission_WhenTitleInvalid()
    {
        await LoginAsCurrentUser();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        await Input(nameof(WorkOrderManage.Elements.Title), "Short");
        await Input(nameof(WorkOrderManage.Elements.Description), "Test description");
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), "101");

        var titleLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.Title));
        var titleValue = await titleLocator.InputValueAsync();
        titleValue.Length.ShouldBeLessThan(12);

        await Input(nameof(WorkOrderManage.Elements.Title), "Valid12Chars");
        
        var updatedTitleValue = await titleLocator.InputValueAsync();
        updatedTitleValue.Length.ShouldBe(12);
    }
}
