﻿using System.Diagnostics;
using System.Globalization;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using System.Text.RegularExpressions;
using ClearMeasure.Bootcamp.Core.Queries;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderSaveDraftTests : AcceptanceTestBase
{
    [Test]
    public async Task ShouldLoadScreenForNewWorkOrder()
    {
        await LoginAsCurrentUser();
        await Page.GetByTestId(nameof(NavMenu.Elements.NewWorkOrder)).ClickAsync();
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");
    }

    [Test]
    public async Task ShouldCreateNewWorkOrderAndVerifyOnSearchScreen()
    {
        await LoginAsCurrentUser();

        WorkOrder order = await CreateAndSaveNewWorkOrder();

        await Page.WaitForURLAsync("**/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await TakeScreenshotAsync(3, "WorkOrderSearchAfterSave");

        Debug.Assert(order.Number != null, "order.Number != null");
        string orderNumber = order.Number;
        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + orderNumber);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Expect(Page).ToHaveURLAsync($"/workorder/manage/{orderNumber}?mode=Edit");
        await TakeScreenshotAsync(5, "WorkOrderManagePage");

        var workOrderNumber = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(workOrderNumber).ToHaveTextAsync(orderNumber);

        var titleField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Title));
        await Expect(titleField).ToHaveValueAsync(order.Title!);

        var descriptionField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Description));
        await Expect(descriptionField).ToHaveValueAsync(order.Description!);

        var instructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions));
        await Expect(instructionsField).ToHaveValueAsync(order.Instructions!);

        var roomNumberField = Page.GetByTestId(nameof(WorkOrderManage.Elements.RoomNumber));
        await Expect(roomNumberField).ToHaveValueAsync(order.RoomNumber!);

        WorkOrder rehyratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number)) ?? throw new InvalidOperationException();
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.CreatedDate)))
            .ToHaveTextAsync(rehyratedOrder.CreatedDate!.Value.ToString(CultureInfo.CurrentCulture));
    }

    [Test]
    public async Task ShouldAssignEmployeeAndSave()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkOrder();

        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await woNumberLocator.WaitForAsync();
        await Expect(woNumberLocator).ToHaveTextAsync(order.Number!);

        await Select(nameof(WorkOrderManage.Elements.Assignee), CurrentUser.UserName);
        await Input(nameof(WorkOrderManage.Elements.Title), "newtitle");
        await Input(nameof(WorkOrderManage.Elements.Description), "newdesc");
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await woNumberLocator.WaitForAsync();
        await Expect(woNumberLocator).ToHaveTextAsync(order.Number!);

        var titleField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Title));
        await Expect(titleField).ToHaveValueAsync("newtitle");

        var descriptionField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Description));
        await Expect(descriptionField).ToHaveValueAsync("newdesc");

        var assigneeField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Assignee));
        await Expect(assigneeField).ToHaveValueAsync(CurrentUser.UserName);

        WorkOrder rehyratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number!)) ?? throw new InvalidOperationException();
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.CreatedDate)))
            .ToHaveTextAsync(rehyratedOrder.CreatedDate!.Value.ToString(CultureInfo.CurrentCulture));
    }

    [Test]
    public async Task ShouldSaveWorkOrderWithInstructionsAt4000Characters()
    {
        await LoginAsCurrentUser();

        await Page.GetByTestId(nameof(NavMenu.Elements.NewWorkOrder)).ClickAsync();
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        var longInstructions = new string('A', 4000);
        
        await Input(nameof(WorkOrderManage.Elements.Title), "Test Long Instructions");
        await Input(nameof(WorkOrderManage.Elements.Description), "Testing 4000 character instructions field");
        await Input(nameof(WorkOrderManage.Elements.Instructions), longInstructions);
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), "R-100");

        var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        var workOrderNumber = await woNumberLocator.InnerTextAsync();

        var saveButtonTestId = nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name;
        await Click(saveButtonTestId);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Navigate back to verify the instructions were saved correctly
        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + workOrderNumber);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var instructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions));
        var savedInstructions = await instructionsField.InputValueAsync();
        
        // Verify the full 4000 characters were saved
        savedInstructions.Length.ShouldBe(4000);
        savedInstructions.ShouldBe(longInstructions);
    }
}