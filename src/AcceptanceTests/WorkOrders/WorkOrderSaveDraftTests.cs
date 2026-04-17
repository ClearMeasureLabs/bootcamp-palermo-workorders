using System.Text.RegularExpressions;
using ClearMeasure.Bootcamp.AcceptanceTests.Extensions;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using Microsoft.Playwright;
using Shouldly;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderSaveDraftTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task ShouldLoadScreenForNewWorkOrder()
    {
        await LoginAsCurrentUser();
        await Page.GetByTestId(nameof(NavMenu.Elements.NewWorkOrder)).ClickAsync();
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");
    }

    [Test, Retry(2)]
    public async Task ShouldCreateNewWorkOrderAndVerifyOnSearchScreen()
    {
        await LoginAsCurrentUser();

        WorkOrder order = await CreateAndSaveNewWorkOrder();

        await Page.WaitForURLAsync("**/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await TakeScreenshotAsync(3, "WorkOrderSearchAfterSave");

        order.Number.ShouldNotBeNullOrWhiteSpace();
        string orderNumber = order.Number;

        var workOrderLink = Page.GetByTestId(nameof(WorkOrderSearch.Elements.WorkOrderLink) + orderNumber);
        await workOrderLink.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 30_000 });
        await TakeScreenshotAsync(4, "WorkOrderLinkVisible");

        await ClickWorkOrderNumberFromSearchPage(order);
        await Expect(Page).ToHaveURLAsync(new Regex($"/workorder/manage/{Regex.Escape(orderNumber)}\\?mode=Edit"));
        await TakeScreenshotAsync(5, "WorkOrderManagePage");

        var workOrderNumber = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(workOrderNumber).ToHaveTextAsync(orderNumber);

        var titleField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Title));
        await Expect(titleField).ToHaveValueAsync(order.Title!);

        var descriptionField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Description));
        await Expect(descriptionField).ToHaveValueAsync(order.Description!);

        var instructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions));
        await Expect(instructionsField).ToHaveValueAsync(order.Instructions ?? "");

        var roomNumberField = Page.GetByTestId(nameof(WorkOrderManage.Elements.RoomNumber));
        await Expect(roomNumberField).ToHaveValueAsync(order.RoomNumber!);

        WorkOrder rehydratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number)) ?? throw new InvalidOperationException();
        var displayedDate = await Page.GetDateTimeFromTestIdAsync(nameof(WorkOrderManage.Elements.CreatedDate));

        rehydratedOrder.CreatedDate.TruncateToMinute().ShouldBe(displayedDate);
        rehydratedOrder.Instructions.ShouldBe(order.Instructions);
    }

    [Test, Retry(2)]
    public async Task ShouldPersistInstructions_When_MaxLengthPlainText()
    {
        await LoginAsCurrentUser();

        var instructions = new string('z', 4000);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        ILocator woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(woNumberLocator).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 30_000 });
        var number = await woNumberLocator.InnerTextAsync();

        await Input(nameof(WorkOrderManage.Elements.Title), $"[{TestTag}] instructions max");
        await Input(nameof(WorkOrderManage.Elements.Description), "desc");
        await Input(nameof(WorkOrderManage.Elements.Instructions), instructions);
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), "101");
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);
        await Page.WaitForURLAsync("**/workorder/search", new PageWaitForURLOptions { Timeout = 90_000 });
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        WorkOrder? saved = null;
        for (var attempt = 0; attempt < 10; attempt++)
        {
            saved = await Bus.Send(new WorkOrderByNumberQuery(number));
            if (saved != null) break;
            await Task.Delay(1000);
        }
        saved.ShouldNotBeNull();
        saved.Instructions.ShouldBe(instructions);

        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        var instructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions));
        await Expect(instructionsField).ToHaveValueAsync(instructions);
    }

    [Test, Retry(2)]
    public async Task ShouldPersistInstructions_When_EmptyThenEditedAfterSave()
    {
        await LoginAsCurrentUser();

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        ILocator woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(woNumberLocator).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 30_000 });
        var number = await woNumberLocator.InnerTextAsync();

        await Input(nameof(WorkOrderManage.Elements.Title), $"[{TestTag}] instructions empty first");
        await Input(nameof(WorkOrderManage.Elements.Description), "desc");
        await Input(nameof(WorkOrderManage.Elements.Instructions), "");
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), "202");
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);
        await Page.WaitForURLAsync("**/workorder/search", new PageWaitForURLOptions { Timeout = 90_000 });
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await woNumberLocator.WaitForAsync();
        await Expect(woNumberLocator).ToHaveTextAsync(number);

        await Select(nameof(WorkOrderManage.Elements.Assignee), CurrentUser.UserName);
        await Input(nameof(WorkOrderManage.Elements.Instructions), "Added after first save");
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);
        await Page.WaitForURLAsync("**/workorder/search", new PageWaitForURLOptions { Timeout = 90_000 });
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var reloaded = await Bus.Send(new WorkOrderByNumberQuery(number)) ?? throw new InvalidOperationException();
        reloaded.Instructions.ShouldBe("Added after first save");
    }

    [Test, Retry(2)]
    public async Task ShouldCreateWorkOrderWith4000CharacterInstructions()
    {
        await LoginAsCurrentUser();

        var instructions = new string('i', 4000);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        ILocator woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(woNumberLocator).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 30_000 });
        var newWorkOrderNumber = await woNumberLocator.InnerTextAsync();

        await Input(nameof(WorkOrderManage.Elements.Title), $"[{TestTag}] long instructions");
        await Input(nameof(WorkOrderManage.Elements.Description), "desc");
        await Input(nameof(WorkOrderManage.Elements.Instructions), instructions);
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), "101");

        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);
        await Page.WaitForURLAsync("**/workorder/search", new PageWaitForURLOptions { Timeout = 90_000 });
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var rehydrated = await Bus.Send(new WorkOrderByNumberQuery(newWorkOrderNumber)) ?? throw new InvalidOperationException();
        rehydrated.Instructions!.Length.ShouldBe(4000);
        rehydrated.Instructions.ShouldBe(instructions);
    }

    [Test, Retry(2)]
    public async Task ShouldCreateWorkOrderWithEmptyInstructions()
    {
        await LoginAsCurrentUser();

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");

        ILocator woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(woNumberLocator).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 30_000 });
        var newWorkOrderNumber = await woNumberLocator.InnerTextAsync();

        await Input(nameof(WorkOrderManage.Elements.Title), $"[{TestTag}] no instructions");
        await Input(nameof(WorkOrderManage.Elements.Description), "only description");
        await Input(nameof(WorkOrderManage.Elements.RoomNumber), "202");

        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);
        await Page.WaitForURLAsync("**/workorder/search", new PageWaitForURLOptions { Timeout = 90_000 });
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var rehydrated = await Bus.Send(new WorkOrderByNumberQuery(newWorkOrderNumber)) ?? throw new InvalidOperationException();
        rehydrated.Instructions.ShouldBeNull();
    }

    [Test, Retry(2)]
    public async Task ShouldPersistInstructionsAfterEditAssign()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkOrder();

        await Page.WaitForURLAsync("**/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var workOrderLink = Page.GetByTestId(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await workOrderLink.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 30_000 });

        await ClickWorkOrderNumberFromSearchPage(order);

        const string addedInstructions = "Added after first save";
        await Input(nameof(WorkOrderManage.Elements.Instructions), addedInstructions);
        await Select(nameof(WorkOrderManage.Elements.Assignee), CurrentUser.UserName);
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + DraftToAssignedCommand.Name);

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await workOrderLink.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 30_000 });
        await ClickWorkOrderNumberFromSearchPage(order);

        var instructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions));
        await Expect(instructionsField).ToHaveValueAsync(addedInstructions);

        var rehydrated = await Bus.Send(new WorkOrderByNumberQuery(order.Number!)) ?? throw new InvalidOperationException();
        rehydrated.Instructions.ShouldBe(addedInstructions);
    }

    [Test, Retry(2)]
    public async Task ShouldAssignEmployeeAndSave()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkOrder();

        await Page.WaitForURLAsync("**/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        order.Number.ShouldNotBeNullOrWhiteSpace();

        var workOrderLink = Page.GetByTestId(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await workOrderLink.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 30_000 });

        await ClickWorkOrderNumberFromSearchPage(order);

        await Select(nameof(WorkOrderManage.Elements.Assignee), CurrentUser.UserName);
        await Input(nameof(WorkOrderManage.Elements.Title), "newtitle");
        await Input(nameof(WorkOrderManage.Elements.Description), "newdesc");
        await Input(nameof(WorkOrderManage.Elements.Instructions), "newinst");
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);

        await Page.WaitForURLAsync("**/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await workOrderLink.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 30_000 });
        await ClickWorkOrderNumberFromSearchPage(order);

        var woNumberLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.WorkOrderNumber));
        await Expect(woNumberLocator).ToHaveTextAsync(order.Number!);

        var titleField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Title));
        await Expect(titleField).ToHaveValueAsync("newtitle");

        var descriptionField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Description));
        await Expect(descriptionField).ToHaveValueAsync("newdesc");

        var instructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions));
        await Expect(instructionsField).ToHaveValueAsync("newinst");

        var assigneeField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Assignee));
        await Expect(assigneeField).ToHaveValueAsync(CurrentUser.UserName);

        WorkOrder rehydratedOrder = await Bus.Send(new WorkOrderByNumberQuery(order.Number!)) ?? throw new InvalidOperationException();
        var displayedDate = await Page.GetDateTimeFromTestIdAsync(nameof(WorkOrderManage.Elements.CreatedDate));

        rehydratedOrder.CreatedDate.TruncateToMinute().ShouldBe(displayedDate);
    }

    [Test, Retry(2)]
    public async Task ShouldCreateWorkOrderWithMaxLengthInstructions()
    {
        await LoginAsCurrentUser();

        var instructions = new string('z', 4000);
        var order = await CreateAndSaveNewWorkOrder(o => o.Instructions = instructions);

        order.Instructions!.Length.ShouldBe(4000);

        await Page.WaitForURLAsync("**/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await ClickWorkOrderNumberFromSearchPage(order);

        var instructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions));
        await Expect(instructionsField).ToHaveValueAsync(instructions);

        var fromDb = await Bus.Send(new WorkOrderByNumberQuery(order.Number!)) ?? throw new InvalidOperationException();
        fromDb.Instructions!.Length.ShouldBe(4000);
        fromDb.Instructions.ShouldBe(instructions);
    }

    [Test, Retry(2)]
    public async Task ShouldCreateWorkOrderWithEmptyInstructions()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkOrder(o => o.Instructions = "");

        order.Instructions.ShouldBeNull();

        await Page.WaitForURLAsync("**/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await ClickWorkOrderNumberFromSearchPage(order);

        var instructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions));
        await Expect(instructionsField).ToHaveValueAsync("");

        var fromDb = await Bus.Send(new WorkOrderByNumberQuery(order.Number!)) ?? throw new InvalidOperationException();
        fromDb.Instructions.ShouldBeNull();
    }

    [Test, Retry(2)]
    public async Task ShouldPersistInstructionsAfterSaveDraftAssignAndReturn()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkOrder(o => o.Instructions = "");
        await Page.WaitForURLAsync("**/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await ClickWorkOrderNumberFromSearchPage(order);
        await Input(nameof(WorkOrderManage.Elements.Instructions), "later added");
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + SaveDraftCommand.Name);
        await Page.WaitForURLAsync("**/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        order = await Bus.Send(new WorkOrderByNumberQuery(order.Number!)) ?? throw new InvalidOperationException();
        order.Instructions.ShouldBe("later added");

        await ClickWorkOrderNumberFromSearchPage(order);
        await AssignExistingWorkOrder(order, CurrentUser.UserName);

        order = await Bus.Send(new WorkOrderByNumberQuery(order.Number!)) ?? throw new InvalidOperationException();
        order.Instructions.ShouldBe("later added");
        order.Status.ShouldBe(WorkOrderStatus.Assigned);

        await ClickWorkOrderNumberFromSearchPage(order);
        var instructionsField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions));
        await Expect(instructionsField).ToHaveValueAsync("later added");
    }
}
