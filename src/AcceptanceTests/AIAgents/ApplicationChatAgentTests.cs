using System.Text.RegularExpressions;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.IntegrationTests;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using NavMenu = ClearMeasure.Bootcamp.UI.Shared.NavMenu;

namespace ClearMeasure.Bootcamp.AcceptanceTests.AIAgents;

/// <summary>
///     Acceptance test for the AI Agent page (/ai-agent).
///     Sends a natural-language prompt through the Blazor UI that instructs the LLM
///     to create and assign a work request, then verifies the database reflects the changes.
/// </summary>
[TestFixture]
public class ApplicationChatAgentTests : AcceptanceTestBase
{
    [SetUp]
    public async Task EnsureLlmAvailable()
    {
        await SkipIfNoChatClient();
    }

    [Test, Ignore("Flaky on CI: MCP loopback ToolProvider produces JSON deserialization error on GitHub Actions runners")]
    public async Task ShouldCreateWorkRequestViaAiAgentChat()
    {
        await LoginAsCurrentUser();

        // Navigate to the AI Agent page
        await Click(nameof(NavMenu.Elements.AiAgent));
        await Page.WaitForURLAsync("**/ai-agent");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Send a prompt that instructs the AI to create a work request and assign it
        const string prompt =
            "I am Timothy Lovejoy (username tlovejoy). " +
            "Create a work request for Groundskeeper Willie (username gwillie) to mow the grass. " +
            "He should take care to edge around the prayer garden. " +
            "Use 'tlovejoy' as the creatorUsername. " +
            "After creating it, assign it to gwillie using the DraftToAssignedCommand " +
            "with executingUsername='tlovejoy' and assigneeUsername='gwillie'. " +
            "In your final response, include the work request number on its own line in exactly this format: " +
            "WorkRequestNumber: <number>";

        await Input(nameof(ApplicationChat.Elements.ChatInput), prompt);
        await Click(nameof(ApplicationChat.Elements.SendButton));

        // Wait for the AI response -- the LLM needs to invoke create-work-request
        // then execute-work-request-command, so allow a generous timeout
        var aiMessage = Page.GetByTestId(nameof(ApplicationChat.Elements.AiMessage) + "1");
        await aiMessage.WaitForAsync(new LocatorWaitForOptions { Timeout = 120_000 });

        // Verify chat history contains a response
        var chatHistory = Page.GetByTestId(nameof(ApplicationChat.Elements.ChatHistory));
        await Expect(chatHistory).ToBeVisibleAsync();
        var chatText = await chatHistory.InnerTextAsync();
        chatText.ShouldNotBeNullOrEmpty();

        // Extract the work request number from the AI response
        var match = Regex.Match(chatText, @"WorkRequestNumber:\s*[`*]*([A-Za-z0-9\-]+)[`*]*");
        match.Success.ShouldBeTrue(
            $"Expected AI response to contain 'WorkRequestNumber: <number>'. Response was: {chatText}");
        var workRequestNumber = match.Groups[1].Value.Trim('`', '*', ' ');

        // Query the database by the specific work request number
        var bus = TestHost.GetRequiredService<IBus>();
        var createdWo = await bus.Send(new WorkRequestByNumberQuery(workRequestNumber));

        // Fallback: if the exact number lookup failed, search all work requests for one
        // created by tlovejoy and assigned to gwillie (LLM may format the number differently)
        if (createdWo == null)
        {
            var allWorkRequests = await bus.Send(new WorkRequestSpecificationQuery());
            createdWo = allWorkRequests
                .FirstOrDefault(wo => wo.Creator?.UserName == "tlovejoy"
                                      && wo.Assignee?.UserName == "gwillie");
        }

        createdWo.ShouldNotBeNull(
            $"Expected a work request with number '{workRequestNumber}' to exist");
        createdWo.Creator!.UserName.ShouldBe("tlovejoy");
        createdWo.Assignee!.UserName.ShouldBe("gwillie");
        createdWo.Status.ShouldBe(WorkRequestStatus.Assigned);
        createdWo.Description.ShouldNotBeNullOrEmpty();

        var description = createdWo.Description!.ToLowerInvariant();
        (description.Contains("edge") || description.Contains("edging") || description.Contains("prayer garden"))
            .ShouldBeTrue(
                $"Expected description to mention edging or prayer garden: {createdWo.Description}");
    }
}
