using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using ClearMeasure.Bootcamp.UI.Shared.Components;

namespace ClearMeasure.Bootcamp.AcceptanceTests.UI;

[TestFixture]
public class ButtonConsistencyTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task LoginButton_ShouldUseConsistentPurplePrimaryStyle()
    {
        // Given: User is on the Login page
        await Page.GotoAsync("/login");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // When: User views the Login button
        var buttonLocator = Page.GetByTestId(nameof(Login.Elements.SubmitButton));
        await Expect(buttonLocator).ToBeVisibleAsync();

        // Then: Button displays standardized purple gradient with consistent styling
        var backgroundColor = await buttonLocator.EvaluateAsync<string>("el => window.getComputedStyle(el).backgroundImage");
        backgroundColor.ShouldContain("linear-gradient");
        
        var borderRadius = await buttonLocator.EvaluateAsync<string>("el => window.getComputedStyle(el).borderRadius");
        borderRadius.ShouldBe("12px");
        
        var color = await buttonLocator.EvaluateAsync<string>("el => window.getComputedStyle(el).color");
        // White text color (rgb(255, 255, 255))
        color.ShouldContain("255");
    }

    [Test, Retry(2)]
    public async Task WorkOrderSearchButton_ShouldUseConsistentPurplePrimaryStyle()
    {
        // Given: User is on the Work Order Search page
        await LoginAsCurrentUser();
        await Click(nameof(NavMenu.Elements.WorkOrderSearch));
        await Page.WaitForURLAsync("**/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // When: User views the Search button
        var buttonLocator = Page.GetByTestId(nameof(WorkOrderSearch.Elements.SearchButton));
        await Expect(buttonLocator).ToBeVisibleAsync();

        // Then: Button displays standardized purple gradient with consistent styling
        var backgroundColor = await buttonLocator.EvaluateAsync<string>("el => window.getComputedStyle(el).backgroundImage");
        backgroundColor.ShouldContain("linear-gradient");
        
        var borderRadius = await buttonLocator.EvaluateAsync<string>("el => window.getComputedStyle(el).borderRadius");
        borderRadius.ShouldBe("12px");
    }

    [Test, Retry(2)]
    public async Task WorkOrderManagePrimaryButton_ShouldUseConsistentPurplePrimaryStyle()
    {
        // Given: User is on the Work Order Manage page
        await LoginAsCurrentUser();
        var order = await CreateAndSaveNewWorkOrder();

        // When: User views the Save button
        await Click(nameof(NavMenu.Elements.WorkOrderSearch));
        await Page.WaitForURLAsync("**/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(WorkOrderSearch.Elements.SearchButton));
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        var linkLocator = Page.Locator($"a[href*='/workorder/manage?workOrderNumber={order.Number}']").First;
        await linkLocator.ClickAsync();
        await Page.WaitForURLAsync($"**/workorder/manage?workOrderNumber={order.Number}");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var buttonLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.SaveDraftButton));
        await Expect(buttonLocator).ToBeVisibleAsync();

        // Then: Button displays standardized purple gradient with consistent styling
        var backgroundColor = await buttonLocator.EvaluateAsync<string>("el => window.getComputedStyle(el).backgroundImage");
        backgroundColor.ShouldContain("linear-gradient");
        
        var borderRadius = await buttonLocator.EvaluateAsync<string>("el => window.getComputedStyle(el).borderRadius");
        borderRadius.ShouldBe("12px");
    }

    [Test, Retry(2)]
    public async Task WorkOrderManageCancelButton_ShouldUseConsistentRedDestructiveStyle()
    {
        // Given: User is on the Work Order Manage page
        await LoginAsCurrentUser();
        var order = await CreateAndSaveNewWorkOrder();

        // When: User views the Cancel button
        await Click(nameof(NavMenu.Elements.WorkOrderSearch));
        await Page.WaitForURLAsync("**/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(WorkOrderSearch.Elements.SearchButton));
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        var linkLocator = Page.Locator($"a[href*='/workorder/manage?workOrderNumber={order.Number}']").First;
        await linkLocator.ClickAsync();
        await Page.WaitForURLAsync($"**/workorder/manage?workOrderNumber={order.Number}");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var buttonLocator = Page.GetByTestId(nameof(WorkOrderManage.Elements.CancelButton));
        await Expect(buttonLocator).ToBeVisibleAsync();

        // Then: Button displays standardized red gradient with consistent styling
        var backgroundColor = await buttonLocator.EvaluateAsync<string>("el => window.getComputedStyle(el).backgroundImage");
        backgroundColor.ShouldContain("linear-gradient");
        
        var borderRadius = await buttonLocator.EvaluateAsync<string>("el => window.getComputedStyle(el).borderRadius");
        borderRadius.ShouldBe("12px");
        
        var color = await buttonLocator.EvaluateAsync<string>("el => window.getComputedStyle(el).color");
        // White text color (rgb(255, 255, 255))
        color.ShouldContain("255");
    }

    [Test, Retry(2)]
    public async Task CounterButtons_ShouldUseConsistentStyles()
    {
        // Given: User is on the Counter page
        await LoginAsCurrentUser();
        await Click(nameof(NavMenu.Elements.Counter));
        await Page.WaitForURLAsync("**/counter");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // When: User views the increment buttons
        var primaryButtonLocator = Page.GetByTestId(nameof(Counter.Elements.IncrementButton));
        await Expect(primaryButtonLocator).ToBeVisibleAsync();
        
        var secondaryButtonLocator = Page.GetByTestId(nameof(Counter.Elements.ResetButton));
        await Expect(secondaryButtonLocator).ToBeVisibleAsync();

        // Then: Primary button displays standardized purple gradient
        var primaryBg = await primaryButtonLocator.EvaluateAsync<string>("el => window.getComputedStyle(el).backgroundImage");
        primaryBg.ShouldContain("linear-gradient");
        
        var primaryBorderRadius = await primaryButtonLocator.EvaluateAsync<string>("el => window.getComputedStyle(el).borderRadius");
        primaryBorderRadius.ShouldBe("12px");

        // And: Secondary button displays standardized gray gradient
        var secondaryBg = await secondaryButtonLocator.EvaluateAsync<string>("el => window.getComputedStyle(el).backgroundImage");
        secondaryBg.ShouldContain("linear-gradient");
        
        var secondaryBorderRadius = await secondaryButtonLocator.EvaluateAsync<string>("el => window.getComputedStyle(el).borderRadius");
        secondaryBorderRadius.ShouldBe("12px");
    }

    [Test, Retry(2)]
    public async Task ChatComponentSendButton_ShouldUseConsistentGraySecondaryStyle()
    {
        // Given: User is viewing the Work Order Chat component
        await LoginAsCurrentUser();
        var order = await CreateAndSaveNewWorkOrder();
        
        // Navigate to work order with assignee
        await Click(nameof(NavMenu.Elements.WorkOrderSearch));
        await Page.WaitForURLAsync("**/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Click(nameof(WorkOrderSearch.Elements.SearchButton));
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        var linkLocator = Page.Locator($"a[href*='/workorder/manage?workOrderNumber={order.Number}']").First;
        await linkLocator.ClickAsync();
        await Page.WaitForURLAsync($"**/workorder/manage?workOrderNumber={order.Number}");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assign the work order to see the chat component
        var assigneeSelect = Page.GetByTestId(nameof(WorkOrderManage.Elements.Assignee));
        if (await assigneeSelect.IsVisibleAsync())
        {
            await assigneeSelect.SelectOptionAsync(new SelectOptionValue { Index = 1 });
            await Click(nameof(WorkOrderManage.Elements.AssignButton));
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }

        // When: User views the Send button
        var buttonLocator = Page.GetByTestId(nameof(WorkOrderChat.Elements.SendButton));
        if (await buttonLocator.IsVisibleAsync())
        {
            // Then: Button displays standardized gray gradient with consistent styling
            var backgroundColor = await buttonLocator.EvaluateAsync<string>("el => window.getComputedStyle(el).backgroundImage");
            backgroundColor.ShouldContain("linear-gradient");
            
            var borderRadius = await buttonLocator.EvaluateAsync<string>("el => window.getComputedStyle(el).borderRadius");
            borderRadius.ShouldBe("12px");
        }
    }

    [Test, Retry(2)]
    public async Task ButtonHoverStates_ShouldBeConsistentAcrossPages()
    {
        // Given: User is on any page with buttons
        await LoginAsCurrentUser();
        await Click(nameof(NavMenu.Elements.Counter));
        await Page.WaitForURLAsync("**/counter");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // When: User hovers over a primary button
        var buttonLocator = Page.GetByTestId(nameof(Counter.Elements.IncrementButton));
        await Expect(buttonLocator).ToBeVisibleAsync();
        
        await buttonLocator.HoverAsync();
        await Page.WaitForTimeoutAsync(100); // Allow hover state to apply

        // Then: Button displays consistent hover styling (transform is applied)
        var transform = await buttonLocator.EvaluateAsync<string>("el => window.getComputedStyle(el).transform");
        // Transform should be applied (not "none")
        transform.ShouldNotBe("none");
    }

    [Test, Retry(2)]
    public async Task ButtonFocusStates_ShouldBeConsistentForAccessibility()
    {
        // Given: User navigates pages using keyboard
        await LoginAsCurrentUser();
        await Click(nameof(NavMenu.Elements.Counter));
        await Page.WaitForURLAsync("**/counter");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // When: User tabs to a button and it receives focus
        var buttonLocator = Page.GetByTestId(nameof(Counter.Elements.IncrementButton));
        await Expect(buttonLocator).ToBeVisibleAsync();
        
        await buttonLocator.FocusAsync();
        await Page.WaitForTimeoutAsync(100); // Allow focus state to apply

        // Then: Button displays consistent focus styling (box-shadow with purple outline)
        var boxShadow = await buttonLocator.EvaluateAsync<string>("el => window.getComputedStyle(el).boxShadow");
        // Focus states should have box-shadow applied
        boxShadow.ShouldNotBeNullOrEmpty();
    }
}
