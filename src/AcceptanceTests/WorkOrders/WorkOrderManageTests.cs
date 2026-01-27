using ClearMeasure.Bootcamp.AcceptanceTests.Extensions;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderManageTests : AcceptanceTestBase
{
    [Test]
    public async Task CreateWorkOrder_WithOnlyLetters_Succeeds()
    {
        await LoginAsCurrentUser();

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        ILocator woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(woNumberLocator).ToBeVisibleAsync();
        var newWorkOrderNumber = await woNumberLocator.InnerTextAsync();

        await Input(nameof(WorkOrderManage.Elements.Title), "ReplaceWindowLatch");
        await Input(nameof(WorkOrderManage.Elements.Description), "Test description");
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), "101");

        var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
        await Click(saveButtonTestId);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Verify work order was created successfully
        WorkOrder? rehyratedOrder = await Bus.Send(new WorkOrderByNumberQuery(newWorkOrderNumber));
        if (rehyratedOrder == null)
        {
            await Task.Delay(1000);
            rehyratedOrder = await Bus.Send(new WorkOrderByNumberQuery(newWorkOrderNumber));
        }
        rehyratedOrder.ShouldNotBeNull();
        rehyratedOrder.Title.ShouldBe("ReplaceWindowLatch");
    }

    [Test]
    public async Task CreateWorkOrder_WithNumbers_ShowsValidationError()
    {
        await LoginAsCurrentUser();

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        ILocator woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(woNumberLocator).ToBeVisibleAsync();
        var newWorkOrderNumber = await woNumberLocator.InnerTextAsync();

        await Input(nameof(WorkOrderManage.Elements.Title), "Replace123Windows");
        await Input(nameof(WorkOrderManage.Elements.Description), "Test description");
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), "101");

        var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
        await Click(saveButtonTestId);

        // Verify validation error appears
        var validationSummary = Page.Locator(".validation-errors");
        await Expect(validationSummary).ToBeVisibleAsync();
        var errorText = await validationSummary.InnerTextAsync();
        errorText.ShouldContain("Title must contain only letters");

        // Verify work order was not created
        WorkOrder? rehyratedOrder = await Bus.Send(new WorkOrderByNumberQuery(newWorkOrderNumber));
        rehyratedOrder.ShouldBeNull();
    }

    [Test]
    public async Task CreateWorkOrder_WithSpecialCharacters_ShowsValidationError()
    {
        await LoginAsCurrentUser();

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        ILocator woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(woNumberLocator).ToBeVisibleAsync();
        var newWorkOrderNumber = await woNumberLocator.InnerTextAsync();

        await Input(nameof(WorkOrderManage.Elements.Title), "Replace-Window!");
        await Input(nameof(WorkOrderManage.Elements.Description), "Test description");
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), "101");

        var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
        await Click(saveButtonTestId);

        // Verify validation error appears
        var validationSummary = Page.Locator(".validation-errors");
        await Expect(validationSummary).ToBeVisibleAsync();
        var errorText = await validationSummary.InnerTextAsync();
        errorText.ShouldContain("Title must contain only letters");

        // Verify work order was not created
        WorkOrder? rehyratedOrder = await Bus.Send(new WorkOrderByNumberQuery(newWorkOrderNumber));
        rehyratedOrder.ShouldBeNull();
    }

    [Test]
    public async Task CreateWorkOrder_WithSpaces_ShowsValidationError()
    {
        await LoginAsCurrentUser();

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        ILocator woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(woNumberLocator).ToBeVisibleAsync();
        var newWorkOrderNumber = await woNumberLocator.InnerTextAsync();

        await Input(nameof(WorkOrderManage.Elements.Title), "Replace Window Latch");
        await Input(nameof(WorkOrderManage.Elements.Description), "Test description");
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), "101");

        var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
        await Click(saveButtonTestId);

        // Verify validation error appears
        var validationSummary = Page.Locator(".validation-errors");
        await Expect(validationSummary).ToBeVisibleAsync();
        var errorText = await validationSummary.InnerTextAsync();
        errorText.ShouldContain("Title must contain only letters");

        // Verify work order was not created
        WorkOrder? rehyratedOrder = await Bus.Send(new WorkOrderByNumberQuery(newWorkOrderNumber));
        rehyratedOrder.ShouldBeNull();
    }

    [Test]
    public async Task EditWorkOrder_ChangingToInvalidTitle_ShowsValidationError()
    {
        await LoginAsCurrentUser();

        // Create work order with valid title
        var order = await CreateAndSaveNewWorkOrder();

        // Navigate to edit page
        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await woNumberLocator.WaitForAsync();
        await Expect(woNumberLocator).ToHaveTextAsync(order.Number!);

        // Change title to include numbers
        await Input(nameof(WorkOrderManage.Elements.Title), "Invalid123Title");
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);

        // Verify validation error appears
        var validationSummary = Page.Locator(".validation-errors");
        await Expect(validationSummary).ToBeVisibleAsync();
        var errorText = await validationSummary.InnerTextAsync();
        errorText.ShouldContain("Title must contain only letters");

        // Verify original title is preserved
        WorkOrder? rehyratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number!));
        rehyratedOrder.ShouldNotBeNull();
        rehyratedOrder.Title.ShouldBe("fromautomation");
    }

    [Test]
    public async Task ViewWorkOrder_WithLegacyInvalidTitle_DisplaysSuccessfully()
    {
        await LoginAsCurrentUser();

        // Create work order with invalid title directly via the Bus (bypass UI validation)
        var order = Faker<WorkOrder>();
        order.Number = null;
        order.Title = "Legacy Title With Spaces 123!";
        order.Description = "Test description";
        order.RoomNumber = "101";
        order.Creator = CurrentUser;

        var command = new SaveDraftCommand(order);
        var result = await Bus.Send(command);
        result.ShouldNotBeNull();

        // Navigate to work order list
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // View the work order with invalid title
        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + result.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Verify work order details display correctly
        var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(woNumberLocator).ToHaveTextAsync(result.Number!);

        var titleField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Title));
        await Expect(titleField).ToHaveValueAsync(result.Title!);

        var descriptionField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Description));
        await Expect(descriptionField).ToHaveValueAsync(result.Description!);
    }
}
