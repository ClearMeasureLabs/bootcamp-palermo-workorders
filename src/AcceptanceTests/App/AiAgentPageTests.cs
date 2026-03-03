using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using Shouldly;

namespace ClearMeasure.Bootcamp.AcceptanceTests.App;

[TestFixture]
public class AiAgentPageTests : AcceptanceTestBase
{
    [SetUp]
    public async Task EnsureLlmAvailable()
    {
        await SkipIfNoChatClient();
    }

    [Test, Retry(2)]
    public async Task ShouldKeepPromptVisibleWhenResizingWithLongConversation()
    {
        await LoginAsCurrentUser();
        await Click(nameof(NavMenu.Elements.AiAgent));
        await Page.WaitForURLAsync("**/ai-agent");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var chatInput = Page.GetByTestId(nameof(ApplicationChat.Elements.ChatInput));
        var sendButton = Page.GetByTestId(nameof(ApplicationChat.Elements.SendButton));
        var history = Page.GetByTestId(nameof(ApplicationChat.Elements.ChatHistoryViewport));

        for (var i = 0; i < 14; i++)
        {
            await Input(nameof(ApplicationChat.Elements.ChatInput), $"Resize test prompt {i} with extra text to force history growth and overflow behavior in the chat viewport.");
            await Click(nameof(ApplicationChat.Elements.SendButton));

            var aiMessageIndex = (i * 2) + 1;
            var aiMessage = Page.GetByTestId($"{nameof(ApplicationChat.Elements.AiMessage)}{aiMessageIndex}");
            await aiMessage.WaitForAsync(new LocatorWaitForOptions { Timeout = 120_000 });
        }

        await Expect(history).ToBeVisibleAsync();
        await AssertPromptControlsAreInViewport(chatInput, sendButton);

        await Page.SetViewportSizeAsync(1440, 900);
        await Page.WaitForTimeoutAsync(150);
        await AssertPromptControlsAreInViewport(chatInput, sendButton);

        await Page.SetViewportSizeAsync(900, 700);
        await Page.WaitForTimeoutAsync(150);
        await AssertPromptControlsAreInViewport(chatInput, sendButton);

        await Page.SetViewportSizeAsync(768, 540);
        await Page.WaitForTimeoutAsync(150);
        await AssertPromptControlsAreInViewport(chatInput, sendButton);

        var canScrollHistory = await history.EvaluateAsync<bool>(
            "node => { const overflowY = window.getComputedStyle(node).overflowY; node.scrollTop = 0; const before = node.scrollTop ?? 0; node.scrollTop = node.scrollHeight; const after = node.scrollTop ?? 0; const hasOverflow = node.scrollHeight > node.clientHeight; const scrollsWhenOverflowed = !hasOverflow || after > before; return overflowY === 'auto' && scrollsWhenOverflowed; }");
        canScrollHistory.ShouldBeTrue();
    }

    private async Task AssertPromptControlsAreInViewport(ILocator chatInput, ILocator sendButton)
    {
        await Expect(chatInput).ToBeVisibleAsync();
        await Expect(sendButton).ToBeVisibleAsync();

        var inputBounds = await chatInput.BoundingBoxAsync();
        var buttonBounds = await sendButton.BoundingBoxAsync();

        inputBounds.ShouldNotBeNull();
        buttonBounds.ShouldNotBeNull();

        var viewportHeight = Page.ViewportSize?.Height ?? 0;
        viewportHeight.ShouldBeGreaterThan(0);

        (inputBounds!.Y + inputBounds.Height <= viewportHeight).ShouldBeTrue();
        (buttonBounds!.Y + buttonBounds.Height <= viewportHeight).ShouldBeTrue();

        var documentScrollY = await Page.EvaluateAsync<int>("() => Math.floor(window.scrollY)");
        documentScrollY.ShouldBe(0);
    }
}
