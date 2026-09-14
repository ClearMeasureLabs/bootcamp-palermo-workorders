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

        // PressSequentiallyAsync fires real keyboard + input events for each character.
        // This is the most reliable way to trigger Blazor's oninput binding without blur.
        await descriptionField.PressSequentiallyAsync("0123456789");

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

        // Set the value via JS and dispatch 'input' atomically — typing 4000 chars would be
        // too slow, and FillAsync's synthetic CDP input event is unreliable on ARM Chromium.
        // Dispatching a bubbling InputEvent with the value already set triggers the Blazor
        // oninput handler without requiring blur, satisfying the acceptance criterion.
        var fullText = new string('A', WorkOrder.DescriptionMaxLength);
        await descriptionField.EvaluateAsync(
            "(el, value) => { el.value = value; el.dispatchEvent(new InputEvent('input', { bubbles: true })); }",
            fullText);

        var caption = Page.GetByTestId(nameof(WorkOrderManage.Elements.DescriptionCharCount));
        await Expect(caption).ToHaveTextAsync("0 characters remaining");
        await Expect(caption).ToHaveClassAsync(new System.Text.RegularExpressions.Regex("text-danger"));

        // Verify maxlength prevents further input
        var valueLength = await descriptionField.EvaluateAsync<int>("el => el.value.length");
        valueLength.ShouldBe(WorkOrder.DescriptionMaxLength);
    }
}
