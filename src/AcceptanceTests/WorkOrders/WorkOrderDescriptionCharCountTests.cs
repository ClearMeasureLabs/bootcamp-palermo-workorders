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

        // Use EvaluateAsync to set the value and fire the input event atomically —
        // same pattern as the ShowsWarning test and WorkOrderSaveDraftTests; reliable on ARM Chromium.
        await descriptionField.EvaluateAsync(
            "(el, value) => { el.value = value; el.dispatchEvent(new Event('input', { bubbles: true })); }",
            "0123456789");

        var caption = Page.GetByTestId(nameof(WorkOrderManage.Elements.DescriptionCharCount));
        await Expect(caption).ToHaveTextAsync("3990 characters remaining");
        await Expect(caption).Not.ToHaveClassAsync(new System.Text.RegularExpressions.Regex("text-danger"));
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

        // Set the full 4000-char value and fire input+change atomically in a single JS call.
        // This mirrors the pattern in WorkOrderSaveDraftTests and is reliable on ARM Chromium
        // because Blazor WASM reads event.target.value when it receives the input event.
        var fullText = new string('A', WorkOrder.DescriptionMaxLength);
        await descriptionField.EvaluateAsync(
            "(el, value) => { el.value = value; el.dispatchEvent(new Event('input', { bubbles: true })); el.dispatchEvent(new Event('change', { bubbles: true })); }",
            fullText);

        var caption = Page.GetByTestId(nameof(WorkOrderManage.Elements.DescriptionCharCount));
        await Expect(caption).ToHaveTextAsync("0 characters remaining");
        await Expect(caption).ToHaveClassAsync(new System.Text.RegularExpressions.Regex("text-danger"));

        // Verify maxlength prevents further input
        var valueLength = await descriptionField.EvaluateAsync<int>("el => el.value.length");
        valueLength.ShouldBe(WorkOrder.DescriptionMaxLength);
    }
}
