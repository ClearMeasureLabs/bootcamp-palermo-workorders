using System.Text.RegularExpressions;
using ClearMeasure.Bootcamp.AcceptanceTests.Extensions;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using Shouldly;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkRequests;

public class WorkRequestSaveDraftTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task ShouldLoadScreenForNewWorkRequest()
    {
        await LoginAsCurrentUser();
        await Page.GetByTestId(nameof(NavMenu.Elements.NewWorkRequest)).ClickAsync();
        await Page.WaitForURLAsync("**/workrequest/manage?mode=New");
    }

    [Test, Retry(2)]
    public async Task ShouldCreateNewWorkRequestAndVerifyOnSearchScreen()
    {
        await LoginAsCurrentUser();

        WorkRequest order = await CreateAndSaveNewWorkRequest();

        await Page.WaitForURLAsync("**/workrequest/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await TakeScreenshotAsync(3, "WorkRequestSearchAfterSave");

        order.Number.ShouldNotBeNullOrWhiteSpace();
        string orderNumber = order.Number;

        var workRequestLink = Page.GetByTestId(nameof(WorkRequestSearch.Elements.WorkRequestLink) + orderNumber);
        await workRequestLink.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 30_000 });
        await TakeScreenshotAsync(4, "WorkRequestLinkVisible");

        await ClickWorkRequestNumberFromSearchPage(order);
        await Expect(Page).ToHaveURLAsync(new Regex($"/workrequest/manage/{Regex.Escape(orderNumber)}\\?mode=Edit"));
        await TakeScreenshotAsync(5, "WorkRequestManagePage");

        var workRequestNumber = Page.GetByTestId(nameof(WorkRequestManage.Elements.WorkRequestNumber));
        await Expect(workRequestNumber).ToHaveTextAsync(orderNumber);

        var titleField = Page.GetByTestId(nameof(WorkRequestManage.Elements.Title));
        await Expect(titleField).ToHaveValueAsync(order.Title!);

        var descriptionField = Page.GetByTestId(nameof(WorkRequestManage.Elements.Description));
        await Expect(descriptionField).ToHaveValueAsync(order.Description!);

        var instructionsField = Page.GetByTestId(nameof(WorkRequestManage.Elements.Instructions));
        await Expect(instructionsField).ToHaveValueAsync(order.Instructions!);

        var roomNumberField = Page.GetByTestId(nameof(WorkRequestManage.Elements.RoomNumber));
        await Expect(roomNumberField).ToHaveValueAsync(order.RoomNumber!);

        WorkRequest rehydratedOrder = await Bus.Send(new WorkRequestByNumberQuery(order.Number)) ?? throw new InvalidOperationException();
        var displayedDate = await Page.GetDateTimeFromTestIdAsync(nameof(WorkRequestManage.Elements.CreatedDate));

        rehydratedOrder.CreatedDate.TruncateToMinute().ShouldBe(displayedDate);
    }

    [Test, Retry(2)]
    public async Task ShouldSaveWorkRequestWithBlankInstructions()
    {
        await LoginAsCurrentUser();

        var order = Faker<WorkRequest>();
        order.Title = $"[{TestTag}] blank instructions";
        order.Number = null;
        order.Instructions = null;

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(NavMenu.Elements.NewWorkRequest));
        await Page.WaitForURLAsync("**/workrequest/manage?mode=New");

        var woNumberLocator = Page.GetByTestId(nameof(WorkRequestManage.Elements.WorkRequestNumber));
        await Expect(woNumberLocator).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 30_000 });
        order.Number = await woNumberLocator.InnerTextAsync();

        await Input(nameof(WorkRequestManage.Elements.Title), order.Title);
        await Input(nameof(WorkRequestManage.Elements.Description), order.Description);
        await Input(nameof(WorkRequestManage.Elements.RoomNumber), order.RoomNumber);

        var saveButtonTestId = nameof(WorkRequestManage.Elements.CommandButton) + SaveDraftCommand.Name;
        await Click(saveButtonTestId);
        await Page.WaitForURLAsync("**/workrequest/search", new PageWaitForURLOptions { Timeout = 90_000 });

        var workRequestLink = Page.GetByTestId(nameof(WorkRequestSearch.Elements.WorkRequestLink) + order.Number);
        await workRequestLink.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 30_000 });
        await ClickWorkRequestNumberFromSearchPage(order);

        var instructionsField = Page.GetByTestId(nameof(WorkRequestManage.Elements.Instructions));
        await Expect(instructionsField).ToHaveValueAsync(string.Empty);

        WorkRequest rehydratedOrder = await Bus.Send(new WorkRequestByNumberQuery(order.Number!)) ?? throw new InvalidOperationException();
        rehydratedOrder.Instructions.ShouldBe(string.Empty);
    }

    [Test, Retry(2)]
    public async Task ShouldAssignEmployeeAndSave()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkRequest();

        await Page.WaitForURLAsync("**/workrequest/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        order.Number.ShouldNotBeNullOrWhiteSpace();

        var workRequestLink = Page.GetByTestId(nameof(WorkRequestSearch.Elements.WorkRequestLink) + order.Number);
        await workRequestLink.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 30_000 });

        await ClickWorkRequestNumberFromSearchPage(order);

        await Select(nameof(WorkRequestManage.Elements.Assignee), CurrentUser.UserName);
        await Input(nameof(WorkRequestManage.Elements.Title), "newtitle");
        await Input(nameof(WorkRequestManage.Elements.Description), "newdesc");
        await Input(nameof(WorkRequestManage.Elements.Instructions), "Check ceiling tile grid before drilling");
        await Click(nameof(WorkRequestManage.Elements.CommandButton) + SaveDraftCommand.Name);

        await Page.WaitForURLAsync("**/workrequest/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await workRequestLink.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 30_000 });
        await ClickWorkRequestNumberFromSearchPage(order);

        var woNumberLocator = Page.GetByTestId(nameof(WorkRequestManage.Elements.WorkRequestNumber));
        await Expect(woNumberLocator).ToHaveTextAsync(order.Number!);

        var titleField = Page.GetByTestId(nameof(WorkRequestManage.Elements.Title));
        await Expect(titleField).ToHaveValueAsync("newtitle");

        var descriptionField = Page.GetByTestId(nameof(WorkRequestManage.Elements.Description));
        await Expect(descriptionField).ToHaveValueAsync("newdesc");

        var instructionsField = Page.GetByTestId(nameof(WorkRequestManage.Elements.Instructions));
        await Expect(instructionsField).ToHaveValueAsync("Check ceiling tile grid before drilling");

        var assigneeField = Page.GetByTestId(nameof(WorkRequestManage.Elements.Assignee));
        await Expect(assigneeField).ToHaveValueAsync(CurrentUser.UserName);

        WorkRequest rehydratedOrder = await Bus.Send(new WorkRequestByNumberQuery(order.Number!)) ?? throw new InvalidOperationException();
        var displayedDate = await Page.GetDateTimeFromTestIdAsync(nameof(WorkRequestManage.Elements.CreatedDate));

        rehydratedOrder.CreatedDate.TruncateToMinute().ShouldBe(displayedDate);
    }
}
