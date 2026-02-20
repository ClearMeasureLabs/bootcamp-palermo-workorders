using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.App;

[TestFixture]
public class ButtonStylingTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task CounterPagePrimaryButton_Should_HaveConsistentStyling()
    {
        await Page.GotoAsync("/counter");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var incrementButton = Page.GetByTestId(nameof(Counter.Elements.IncrementButton));
        await Expect(incrementButton).ToBeVisibleAsync();

        var styles = await incrementButton.EvaluateAsync<Dictionary<string, string>>(@"
            button => {
                const computed = window.getComputedStyle(button);
                return {
                    padding: computed.padding,
                    borderRadius: computed.borderRadius,
                    backgroundImage: computed.backgroundImage,
                    color: computed.color,
                    transition: computed.transition
                };
            }
        ");

        styles["padding"].ShouldContain("16px 32px");
        styles["borderRadius"].ShouldBe("12px");
        styles["backgroundImage"].ShouldContain("linear-gradient");
        styles["color"].ShouldBe("rgb(255, 255, 255)");
        styles["transition"].ShouldContain("0.3s");
    }

    [Test, Retry(2)]
    public async Task CounterPageSecondaryButton_Should_HaveConsistentStyling()
    {
        await Page.GotoAsync("/counter");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var resetButton = Page.GetByTestId(nameof(Counter.Elements.ResetButton));
        await Expect(resetButton).ToBeVisibleAsync();

        var styles = await resetButton.EvaluateAsync<Dictionary<string, string>>(@"
            button => {
                const computed = window.getComputedStyle(button);
                return {
                    padding: computed.padding,
                    borderRadius: computed.borderRadius,
                    backgroundImage: computed.backgroundImage,
                    transition: computed.transition
                };
            }
        ");

        styles["padding"].ShouldContain("16px 32px");
        styles["borderRadius"].ShouldBe("12px");
        styles["backgroundImage"].ShouldContain("linear-gradient");
        styles["transition"].ShouldContain("0.3s");
    }

    [Test, Retry(2)]
    public async Task LoginPagePrimaryButton_Should_HaveConsistentStyling()
    {
        await Page.GotoAsync("/login");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var loginButton = Page.GetByTestId(nameof(Login.Elements.LoginButton));
        await Expect(loginButton).ToBeVisibleAsync();

        var styles = await loginButton.EvaluateAsync<Dictionary<string, string>>(@"
            button => {
                const computed = window.getComputedStyle(button);
                return {
                    padding: computed.padding,
                    borderRadius: computed.borderRadius,
                    backgroundImage: computed.backgroundImage,
                    color: computed.color,
                    transition: computed.transition
                };
            }
        ");

        styles["padding"].ShouldContain("16px 32px");
        styles["borderRadius"].ShouldBe("12px");
        styles["backgroundImage"].ShouldContain("linear-gradient");
        styles["color"].ShouldBe("rgb(255, 255, 255)");
        styles["transition"].ShouldContain("0.3s");
    }

    [Test, Retry(2)]
    public async Task WorkOrderManagePagePrimaryButton_Should_HaveConsistentStyling()
    {
        await LoginAsCurrentUser();
        var workOrder = await CreateAndSaveNewWorkOrder();
        await Page.GotoAsync($"/workorder/{workOrder.Number}");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var saveButton = Page.GetByTestId(nameof(WorkOrderManage.Elements.SaveButton));
        await Expect(saveButton).ToBeVisibleAsync();

        var styles = await saveButton.EvaluateAsync<Dictionary<string, string>>(@"
            button => {
                const computed = window.getComputedStyle(button);
                return {
                    padding: computed.padding,
                    borderRadius: computed.borderRadius,
                    backgroundImage: computed.backgroundImage,
                    color: computed.color,
                    transition: computed.transition
                };
            }
        ");

        styles["padding"].ShouldContain("16px 32px");
        styles["borderRadius"].ShouldBe("12px");
        styles["backgroundImage"].ShouldContain("linear-gradient");
        styles["color"].ShouldBe("rgb(255, 255, 255)");
        styles["transition"].ShouldContain("0.3s");
    }

    [Test, Retry(2)]
    public async Task WorkOrderManagePageRedButton_Should_HaveConsistentStyling()
    {
        await LoginAsCurrentUser();
        var workOrder = await CreateAndSaveNewWorkOrder();
        await Page.GotoAsync($"/workorder/{workOrder.Number}");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var cancelButton = Page.GetByTestId(nameof(WorkOrderManage.Elements.CancelButton));
        await Expect(cancelButton).ToBeVisibleAsync();

        var styles = await cancelButton.EvaluateAsync<Dictionary<string, string>>(@"
            button => {
                const computed = window.getComputedStyle(button);
                return {
                    padding: computed.padding,
                    borderRadius: computed.borderRadius,
                    backgroundImage: computed.backgroundImage,
                    color: computed.color,
                    transition: computed.transition
                };
            }
        ");

        styles["padding"].ShouldContain("16px 32px");
        styles["borderRadius"].ShouldBe("12px");
        styles["backgroundImage"].ShouldContain("linear-gradient");
        styles["color"].ShouldBe("rgb(255, 255, 255)");
        styles["transition"].ShouldContain("0.3s");
    }

    [Test, Retry(2)]
    public async Task WorkOrderSearchPagePrimaryButton_Should_HaveConsistentStyling()
    {
        await LoginAsCurrentUser();
        await Page.GotoAsync("/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var searchButton = Page.Locator("#SearchButton");
        await Expect(searchButton).ToBeVisibleAsync();

        var styles = await searchButton.EvaluateAsync<Dictionary<string, string>>(@"
            button => {
                const computed = window.getComputedStyle(button);
                return {
                    padding: computed.padding,
                    borderRadius: computed.borderRadius,
                    backgroundImage: computed.backgroundImage,
                    color: computed.color,
                    transition: computed.transition
                };
            }
        ");

        styles["padding"].ShouldContain("16px 32px");
        styles["borderRadius"].ShouldBe("12px");
        styles["backgroundImage"].ShouldContain("linear-gradient");
        styles["color"].ShouldBe("rgb(255, 255, 255)");
        styles["transition"].ShouldContain("0.3s");
    }

    [Test, Retry(2)]
    public async Task ButtonHoverState_Should_TransitionSmoothly()
    {
        await Page.GotoAsync("/counter");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var incrementButton = Page.GetByTestId(nameof(Counter.Elements.IncrementButton));
        await Expect(incrementButton).ToBeVisibleAsync();

        await incrementButton.HoverAsync();
        await Page.WaitForTimeoutAsync(500);

        var hoverStyles = await incrementButton.EvaluateAsync<Dictionary<string, string>>(@"
            button => {
                const computed = window.getComputedStyle(button);
                return {
                    backgroundImage: computed.backgroundImage,
                    boxShadow: computed.boxShadow
                };
            }
        ");

        hoverStyles["backgroundImage"].ShouldContain("linear-gradient");
        hoverStyles["boxShadow"].ShouldNotBeNullOrEmpty();
    }

    [Test, Retry(2)]
    public async Task ButtonKeyboardFocus_Should_DisplayFocusRing()
    {
        await Page.GotoAsync("/counter");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var incrementButton = Page.GetByTestId(nameof(Counter.Elements.IncrementButton));
        await incrementButton.FocusAsync();

        var focusStyles = await incrementButton.EvaluateAsync<Dictionary<string, string>>(@"
            button => {
                const computed = window.getComputedStyle(button);
                return {
                    boxShadow: computed.boxShadow,
                    outline: computed.outline
                };
            }
        ");

        (focusStyles["boxShadow"] != "none" || focusStyles["outline"] != "none").ShouldBeTrue();
    }
}
