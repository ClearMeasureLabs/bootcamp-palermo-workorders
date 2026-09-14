using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderDescriptionCharCountTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task DescriptionCharCount_InitialCaption_ShowsFullLimit()
    {
        await LoginAsCurrentUser();

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");
        await WaitForNewWorkOrderFormReadyAsync();

        var caption = Page.GetByTestId(nameof(WorkOrderManage.Elements.DescriptionCharCount));
        await Expect(caption).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 15_000 });
        await Expect(caption).ToHaveTextAsync("4000 characters remaining");
    }

    [Test, Retry(2)]
    public async Task DescriptionCharCount_UpdatesCaption_AsUserTypes()
    {
        await LoginAsCurrentUser();

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");
        await WaitForNewWorkOrderFormReadyAsync();

        var descriptionField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Description));
        await Expect(descriptionField).ToBeEditableAsync(new LocatorAssertionsToBeEditableOptions { Timeout = 30_000 });

        // FillAsync sets the value and fires input events, triggering Blazor's oninput binding.
        // Assert WITHOUT blur — the caption must update while focus remains in the field.
        await descriptionField.FillAsync("0123456789");

        var caption = Page.GetByTestId(nameof(WorkOrderManage.Elements.DescriptionCharCount));
        await Expect(caption).ToHaveTextAsync("3990 characters remaining",
            new LocatorAssertionsToHaveTextOptions { Timeout = 15_000 });
    }

    [Test, Retry(2)]
    public async Task DescriptionCharCount_ShowsWarning_WhenLimitReached()
    {
        await LoginAsCurrentUser();

        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(NavMenu.Elements.NewWorkOrder));
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");
        await WaitForNewWorkOrderFormReadyAsync();

        var descriptionField = Page.GetByTestId(nameof(WorkOrderManage.Elements.Description));
        await Expect(descriptionField).ToBeEditableAsync(new LocatorAssertionsToBeEditableOptions { Timeout = 30_000 });

        // FillAsync sets the full 4000-char value and fires input events, triggering oninput.
        // Assert WITHOUT blur — the caption and text-danger class must appear during typing.
        var fullText = new string('A', WorkOrder.DescriptionMaxLength);
        await descriptionField.FillAsync(fullText);

        var caption = Page.GetByTestId(nameof(WorkOrderManage.Elements.DescriptionCharCount));
        await Expect(caption).ToHaveTextAsync("0 characters remaining",
            new LocatorAssertionsToHaveTextOptions { Timeout = 15_000 });
        await Expect(caption).ToHaveClassAsync(new System.Text.RegularExpressions.Regex("text-danger"),
            new LocatorAssertionsToHaveClassOptions { Timeout = 15_000 });
    }
}
