using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.App;

[TestFixture]
public class ButtonStylingTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task CounterPage_PrimaryButton_Should_HaveConsistentStyling()
    {
        await Page.GotoAsync("/counter");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var button = Page.GetByTestId(nameof(Counter.Elements.IncrementButton));
        await Expect(button).ToBeVisibleAsync();

        var styles = await button.EvaluateAsync<Dictionary<string, string>>(@"
            (element) => {
                const computed = window.getComputedStyle(element);
                return {
                    background: computed.backgroundImage,
                    padding: computed.padding,
                    borderRadius: computed.borderRadius,
                    boxShadow: computed.boxShadow,
                    transition: computed.transition,
                    color: computed.color
                };
            }
        ");

        styles["background"].Should().Contain("linear-gradient");
        styles["background"].Should().Contain("rgb(123, 104, 238)");
        styles["background"].Should().Contain("rgb(74, 144, 226)");
        styles["padding"].Should().Contain("16px 32px");
        styles["borderRadius"].Should().Contain("12px");
        styles["boxShadow"].Should().Contain("rgba(123, 104, 238, 0.3)");
        styles["transition"].Should().Contain("0.3s");
        styles["color"].Should().Be("rgb(255, 255, 255)");
    }

    [Test, Retry(2)]
    public async Task CounterPage_SecondaryButton_Should_HaveConsistentStyling()
    {
        await Page.GotoAsync("/counter");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var button = Page.GetByTestId(nameof(Counter.Elements.ResetButton));
        await Expect(button).ToBeVisibleAsync();

        var styles = await button.EvaluateAsync<Dictionary<string, string>>(@"
            (element) => {
                const computed = window.getComputedStyle(element);
                return {
                    background: computed.backgroundImage,
                    padding: computed.padding,
                    borderRadius: computed.borderRadius,
                    boxShadow: computed.boxShadow,
                    transition: computed.transition
                };
            }
        ");

        styles["background"].Should().Contain("linear-gradient");
        styles["background"].Should().Contain("rgb(226, 232, 240)");
        styles["background"].Should().Contain("rgb(203, 213, 224)");
        styles["padding"].Should().Contain("16px 32px");
        styles["borderRadius"].Should().Contain("12px");
        styles["boxShadow"].Should().Contain("rgba(0, 0, 0, 0.1)");
        styles["transition"].Should().Contain("0.3s");
    }

    [Test, Retry(2)]
    public async Task LoginPage_PrimaryButton_Should_HaveConsistentStyling()
    {
        await Page.GotoAsync("/");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var button = Page.GetByTestId(nameof(Login.Elements.LoginButton));
        await Expect(button).ToBeVisibleAsync();

        var styles = await button.EvaluateAsync<Dictionary<string, string>>(@"
            (element) => {
                const computed = window.getComputedStyle(element);
                return {
                    background: computed.backgroundImage,
                    padding: computed.padding,
                    borderRadius: computed.borderRadius,
                    boxShadow: computed.boxShadow,
                    color: computed.color
                };
            }
        ");

        styles["background"].Should().Contain("linear-gradient");
        styles["background"].Should().Contain("rgb(123, 104, 238)");
        styles["background"].Should().Contain("rgb(74, 144, 226)");
        styles["padding"].Should().Contain("16px 32px");
        styles["borderRadius"].Should().Contain("12px");
        styles["boxShadow"].Should().Contain("rgba(123, 104, 238, 0.3)");
        styles["color"].Should().Be("rgb(255, 255, 255)");
    }

    [Test, Retry(2)]
    public async Task WorkOrderManagePage_PrimaryButton_Should_HaveConsistentStyling()
    {
        await LoginAsCurrentUser();
        var workOrder = await CreateAndSaveNewWorkOrder();
        await Page.GotoAsync($"/workorder/{workOrder.Number}");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var buttons = await Page.Locator("button.btn-primary").AllAsync();
        buttons.Count.Should().BeGreaterThan(0);

        foreach (var button in buttons)
        {
            var styles = await button.EvaluateAsync<Dictionary<string, string>>(@"
                (element) => {
                    const computed = window.getComputedStyle(element);
                    return {
                        background: computed.backgroundImage,
                        padding: computed.padding,
                        borderRadius: computed.borderRadius,
                        boxShadow: computed.boxShadow,
                        transition: computed.transition,
                        color: computed.color
                    };
                }
            ");

            styles["background"].Should().Contain("linear-gradient");
            styles["background"].Should().Contain("rgb(123, 104, 238)");
            styles["background"].Should().Contain("rgb(74, 144, 226)");
            styles["padding"].Should().Contain("16px 32px");
            styles["borderRadius"].Should().Contain("12px");
            styles["boxShadow"].Should().Contain("rgba(123, 104, 238, 0.3)");
            styles["transition"].Should().Contain("0.3s");
            styles["color"].Should().Be("rgb(255, 255, 255)");
        }
    }

    [Test, Retry(2)]
    public async Task WorkOrderManagePage_RedButton_Should_HaveConsistentStyling()
    {
        await LoginAsCurrentUser();
        var workOrder = await CreateAndSaveNewWorkOrder();
        await Page.GotoAsync($"/workorder/{workOrder.Number}");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var redButtons = await Page.Locator("button.btn-red").AllAsync();
        
        if (redButtons.Count > 0)
        {
            var button = redButtons.First();
            var styles = await button.EvaluateAsync<Dictionary<string, string>>(@"
                (element) => {
                    const computed = window.getComputedStyle(element);
                    return {
                        background: computed.backgroundImage,
                        padding: computed.padding,
                        borderRadius: computed.borderRadius,
                        boxShadow: computed.boxShadow,
                        transition: computed.transition,
                        color: computed.color
                    };
                }
            ");

            styles["background"].Should().Contain("linear-gradient");
            styles["background"].Should().Contain("rgb(238, 104, 104)");
            styles["background"].Should().Contain("rgb(226, 74, 140)");
            styles["padding"].Should().Contain("16px 32px");
            styles["borderRadius"].Should().Contain("12px");
            styles["boxShadow"].Should().Contain("rgba(238, 104, 104, 0.3)");
            styles["transition"].Should().Contain("0.3s");
            styles["color"].Should().Be("rgb(255, 255, 255)");
        }
    }

    [Test, Retry(2)]
    public async Task WorkOrderSearchPage_PrimaryButton_Should_HaveConsistentStyling()
    {
        await LoginAsCurrentUser();
        await Page.GotoAsync("/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var button = Page.Locator("#SearchButton");
        await Expect(button).ToBeVisibleAsync();

        var styles = await button.EvaluateAsync<Dictionary<string, string>>(@"
            (element) => {
                const computed = window.getComputedStyle(element);
                return {
                    background: computed.backgroundImage,
                    padding: computed.padding,
                    borderRadius: computed.borderRadius,
                    boxShadow: computed.boxShadow,
                    transition: computed.transition,
                    color: computed.color
                };
            }
        ");

        styles["background"].Should().Contain("linear-gradient");
        styles["background"].Should().Contain("rgb(123, 104, 238)");
        styles["background"].Should().Contain("rgb(74, 144, 226)");
        styles["padding"].Should().Contain("16px 32px");
        styles["borderRadius"].Should().Contain("12px");
        styles["boxShadow"].Should().Contain("rgba(123, 104, 238, 0.3)");
        styles["transition"].Should().Contain("0.3s");
        styles["color"].Should().Be("rgb(255, 255, 255)");
    }

    [Test, Retry(2)]
    public async Task PrimaryButton_Hover_Should_HaveConsistentStyling()
    {
        await Page.GotoAsync("/counter");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var button = Page.GetByTestId(nameof(Counter.Elements.IncrementButton));
        await Expect(button).ToBeVisibleAsync();

        await button.HoverAsync();
        await Page.WaitForTimeoutAsync(500);

        var stylesOnHover = await button.EvaluateAsync<Dictionary<string, string>>(@"
            (element) => {
                const computed = window.getComputedStyle(element);
                return {
                    background: computed.backgroundImage,
                    boxShadow: computed.boxShadow,
                    transform: computed.transform
                };
            }
        ");

        stylesOnHover["background"].Should().Contain("linear-gradient");
        stylesOnHover["background"].Should().Contain("rgb(106, 90, 205)");
        stylesOnHover["background"].Should().Contain("rgb(65, 105, 225)");
        stylesOnHover["boxShadow"].Should().Contain("rgba(123, 104, 238, 0.4)");
        stylesOnHover["transform"].Should().Contain("translateY");
    }

    [Test, Retry(2)]
    public async Task Button_KeyboardFocus_Should_HaveAccessibleFocusRing()
    {
        await Page.GotoAsync("/counter");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var button = Page.GetByTestId(nameof(Counter.Elements.IncrementButton));
        await Expect(button).ToBeVisibleAsync();

        await button.FocusAsync();
        await Page.WaitForTimeoutAsync(300);

        var focusStyles = await button.EvaluateAsync<Dictionary<string, string>>(@"
            (element) => {
                const computed = window.getComputedStyle(element);
                return {
                    boxShadow: computed.boxShadow,
                    outline: computed.outline
                };
            }
        ");

        var hasFocusIndicator = focusStyles["boxShadow"].Contains("rgb(123, 104, 238)") ||
                                !string.IsNullOrEmpty(focusStyles["outline"]) && focusStyles["outline"] != "none";
        
        hasFocusIndicator.Should().BeTrue("Button should have visible focus indicator for keyboard accessibility");
    }
}
