using System.Text.RegularExpressions;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.IntegrationTests;
using ClearMeasure.Bootcamp.LlmGateway;
using Shouldly;

namespace ClearMeasure.Bootcamp.AcceptanceTests.McpServer;

[TestFixture]
public class McpChatConversationTests : AcceptanceTestBase
{
	protected override bool RequiresBrowser => false;

	private static McpTestHelper? _helper;

	[OneTimeSetUp]
	public async Task McpSetUp()
	{
		_helper = new McpTestHelper(TestHost.GetRequiredService<ChatClientFactory>());
		await _helper.ConnectAsync();
	}

	[OneTimeTearDown]
	public async Task McpTearDown()
	{
		if (_helper != null) await _helper.DisposeAsync();
	}

	[SetUp]
	public async Task EnsureAvailability()
	{
		if (!_helper!.Connected)
			Assert.Inconclusive("MCP HTTP server is not available");
		await SkipIfNoChatClient();
	}

	[Test, Retry(2)]
	public async Task ShouldCreateAndAssignWorkRequestFromConversationalPrompt()
	{
		var response = await _helper!.SendPrompt(
			"I am Timothy Lovejoy (my username is tlovejoy). " +
			"Create a new work request assigned to Groundskeeper Willie (username gwillie) " +
			"to cut the grass and make sure that the edging is done and that fertilizer is put down. " +
			"This will be on the outdoor lawn. " +
			"Steps to follow:\n" +
			"1. Call create-work-request with a suitable title, a description that captures the full scope of work " +
			"(cutting grass, edging, and fertilizer), creatorUsername='tlovejoy', and roomNumber='Outdoor Lawn'.\n" +
			"2. Take the work request Number returned from step 1 and call execute-work-request-command with " +
			"commandName='DraftToAssignedCommand', executingUsername='tlovejoy', assigneeUsername='gwillie'.\n" +
			"In your final response, include the work request number on its own line in exactly this format: " +
			"WorkRequestNumber: <number>");

		response.Text.ShouldNotBeNullOrEmpty();

		var match = Regex.Match(response.Text, @"WorkRequestNumber:\s*(\S+)");
		match.Success.ShouldBeTrue(
			$"Expected response to contain 'WorkRequestNumber: <number>'. Response was: {response.Text}");
		var workRequestNumber = match.Groups[1].Value;

		var bus = TestHost.GetRequiredService<IBus>();
		var lawnWorkRequest = await bus.Send(new WorkRequestByNumberQuery(workRequestNumber));

		lawnWorkRequest.ShouldNotBeNull(
			$"Expected a work request with number '{workRequestNumber}' to exist");
		lawnWorkRequest.Status.ShouldBe(WorkRequestStatus.Assigned);
		lawnWorkRequest.Creator!.UserName.ShouldBe("tlovejoy");
		lawnWorkRequest.Assignee!.UserName.ShouldBe("gwillie");
		lawnWorkRequest.Title.ShouldNotBeNullOrEmpty();
		lawnWorkRequest.Description.ShouldNotBeNullOrEmpty();
		var description = lawnWorkRequest.Description!.ToLowerInvariant();
		description.ShouldContain("grass");
		(description.Contains("edging") || description.Contains("edge"))
			.ShouldBeTrue($"Expected description to mention edging or edges: {lawnWorkRequest.Description}");
		description.ShouldContain("fertilizer");
		lawnWorkRequest.RoomNumber.ShouldNotBeNullOrEmpty(
			"Room number should be set to a value representing the outdoor lawn");
	}
}
