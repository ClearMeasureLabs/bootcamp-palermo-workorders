using ClearMeasure.Bootcamp.AcceptanceTests.Extensions;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkRequests;

public class WorkRequestCompleteTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task ShouldCompleteWorkRequest()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkRequest();
        order = await ClickWorkRequestNumberFromSearchPage(order);
        order = await AssignExistingWorkRequest(order, CurrentUser.UserName);
        order = await ClickWorkRequestNumberFromSearchPage(order);

        order = await BeginExistingWorkRequest(order);
        order = await ClickWorkRequestNumberFromSearchPage(order);

        var expectedTitle = "Title from automation";
        var expectedDescription = "Description";
        var expectedInstructions = "Bring ladder and safety gear";
        order.Title = expectedTitle;
        order.Description = expectedDescription;
        order.Instructions = expectedInstructions;
        order = await CompleteExistingWorkRequest(order);
        order = await ClickWorkRequestNumberFromSearchPage(order);

        await Expect(Page.GetByTestId(nameof(WorkRequestManage.Elements.Title))).ToHaveValueAsync(expectedTitle,
            new LocatorAssertionsToHaveValueOptions
            {
                Timeout = 10000 // 10 seconds
            });

        await Expect(Page.GetByTestId(nameof(WorkRequestManage.Elements.Title))).ToBeDisabledAsync();
        await Expect(Page.GetByTestId(nameof(WorkRequestManage.Elements.Description)))
            .ToHaveValueAsync(expectedDescription);
        await Expect(Page.GetByTestId(nameof(WorkRequestManage.Elements.Description))).ToBeDisabledAsync();
        await Expect(Page.GetByTestId(nameof(WorkRequestManage.Elements.Instructions)))
            .ToHaveValueAsync(expectedInstructions);
        await Expect(Page.GetByTestId(nameof(WorkRequestManage.Elements.Instructions))).ToBeDisabledAsync();
        await Expect(Page.GetByTestId(nameof(WorkRequestManage.Elements.Status)))
            .ToHaveTextAsync(WorkRequestStatus.Complete.FriendlyName);


        var displayedDateTime = await Page.GetDateTimeFromTestIdAsync(nameof(WorkRequestManage.Elements.CompletedDate));

        var rehyratedOrder = await Bus.Send(new WorkRequestByNumberQuery(order.Number!)) ??
                             throw new InvalidOperationException();
        rehyratedOrder.CompletedDate.TruncateToMinute().ShouldBe(displayedDateTime);
    }

    [Test, Retry(2)]
    public async Task CompleteWorkRequestWorkflow()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkRequest();
        order = await ClickWorkRequestNumberFromSearchPage(order);

        order = await AssignExistingWorkRequest(order, CurrentUser.UserName);
        order = await ClickWorkRequestNumberFromSearchPage(order);

        order = await BeginExistingWorkRequest(order);
        order = await ClickWorkRequestNumberFromSearchPage(order);

        order = await CompleteExistingWorkRequest(order);
        order = await ClickWorkRequestNumberFromSearchPage(order);

        var rehyratedOrder = await Bus.Send(new WorkRequestByNumberQuery(order.Number!)) ??
                             throw new InvalidOperationException();
        rehyratedOrder.Status.ShouldBe(WorkRequestStatus.Complete);

        await Expect(Page.GetByTestId(nameof(WorkRequestManage.Elements.ReadOnlyMessage)))
            .ToHaveTextAsync("This work request is read-only for you at this time.");
    }
}