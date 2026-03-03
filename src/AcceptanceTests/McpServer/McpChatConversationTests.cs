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
	public async Task ShouldCreateAndAssignWorkOrderFromConversationalPrompt()
	{
		var response = await _helper!.SendPrompt(
			"I am Timothy Lovejoy (my username is tlovejoy). " +
			"Create a new work order assigned to Groundskeeper Willie (username gwillie) " +
			"to cut the grass and make sure that the edging is done and that fertilizer is put down. " +
			"This will be on the outdoor lawn. " +
			"Steps to follow:\n" +
			"1. Call create-work-order with a suitable title, a description that captures the full scope of work " +
			"(cutting grass, edging, and fertilizer), creatorUsername='tlovejoy', and roomNumber='Outdoor Lawn'.\n" +
			"2. Take the work order Number returned from step 1 and call execute-work-order-command with " +
			"commandName='DraftToAssignedCommand', executingUsername='tlovejoy', assigneeUsername='gwillie'.\n" +
			"In your final response, include the work order number on its own line in exactly this format: " +
			"WorkOrderNumber: <number>");

		response.Text.ShouldNotBeNullOrEmpty();

		var match = Regex.Match(response.Text, @"WorkOrderNumber:\s*(\S+)");
		match.Success.ShouldBeTrue(
			$"Expected response to contain 'WorkOrderNumber: <number>'. Response was: {response.Text}");
		var workOrderNumber = match.Groups[1].Value;

		var bus = TestHost.GetRequiredService<IBus>();
		var lawnWorkOrder = await bus.Send(new WorkOrderByNumberQuery(workOrderNumber));

		lawnWorkOrder.ShouldNotBeNull(
			$"Expected a work order with number '{workOrderNumber}' to exist");
		lawnWorkOrder.Status.ShouldBe(WorkOrderStatus.Assigned);
		lawnWorkOrder.Creator!.UserName.ShouldBe("tlovejoy");
		lawnWorkOrder.Assignee!.UserName.ShouldBe("gwillie");
		lawnWorkOrder.Title.ShouldNotBeNullOrEmpty();
		lawnWorkOrder.Description.ShouldNotBeNullOrEmpty();
		var description = lawnWorkOrder.Description!.ToLowerInvariant();
		description.ShouldContain("grass");
		(description.Contains("edging") || description.Contains("edge"))
			.ShouldBeTrue($"Expected description to mention edging or edges: {lawnWorkOrder.Description}");
		description.ShouldContain("fertilizer");
		lawnWorkOrder.RoomNumber.ShouldNotBeNullOrEmpty(
			"Room number should be set to a value representing the outdoor lawn");
	}
}
