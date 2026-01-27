using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.App;

[TestFixture]
public class AppTests : AcceptanceTestBase
{
    [Test]
    public async Task AllTextInputs_AcrossApplication_HaveConsistentBlinkingAnimation()
    {
        await LoginAsCurrentUser();

        // Navigate to Create Work Order page
        await Page.GetByTestId(nameof(NavMenu.Elements.NewWorkOrder)).ClickAsync();
        await Page.WaitForURLAsync("**/workorder/manage?mode=New");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Verify all text inputs on Create page have blinking animation
        var textInputs = await Page.Locator(".input-text, .input-textarea").AllAsync();
        textInputs.Count.ShouldBeGreaterThan(0);

        foreach (var input in textInputs)
        {
            var animationName = await input.EvaluateAsync<string>("element => window.getComputedStyle(element).animationName");
            animationName.ShouldBe("blinking-border");
        }

        // Navigate to Work Orders list page
        await Page.GotoAsync("/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Create and navigate to Edit page
        var order = await CreateAndSaveNewWorkOrder();
        await Click(nameof(WorkOrderSearch.Elements.WorkOrderLink) + order.Number);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Verify all text inputs on Edit page have blinking animation
        var editTextInputs = await Page.Locator(".input-text, .input-textarea").AllAsync();
        editTextInputs.Count.ShouldBeGreaterThan(0);

        foreach (var input in editTextInputs)
        {
            var animationName = await input.EvaluateAsync<string>("element => window.getComputedStyle(element).animationName");
            animationName.ShouldBe("blinking-border");

            // Verify animation timing is consistent (2s duration)
            var animationDuration = await input.EvaluateAsync<string>("element => window.getComputedStyle(element).animationDuration");
            animationDuration.ShouldBe("2s");

            // Verify animation is infinite
            var animationIterationCount = await input.EvaluateAsync<string>("element => window.getComputedStyle(element).animationIterationCount");
            animationIterationCount.ShouldBe("infinite");
        }
    }
}
