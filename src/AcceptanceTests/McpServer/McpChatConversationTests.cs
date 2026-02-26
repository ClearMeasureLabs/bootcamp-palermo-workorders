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
			"Report the final work order details including the number, title, description, status, assignee, and room number.");

		response.Text.ShouldNotBeNullOrEmpty();

		var bus = TestHost.GetRequiredService<IBus>();
		var workOrders = await bus.Send(new WorkOrderSpecificationQuery());

		var lawnWorkOrder = workOrders.FirstOrDefault(wo =>
			wo.Assignee?.UserName == "gwillie" &&
			wo.Status == WorkOrderStatus.Assigned &&
			wo.Creator?.UserName == "tlovejoy" &&
			(wo.Title!.Contains("grass", StringComparison.OrdinalIgnoreCase) ||
			 wo.Title!.Contains("lawn", StringComparison.OrdinalIgnoreCase) ||
			 wo.Description!.Contains("grass", StringComparison.OrdinalIgnoreCase)));

		lawnWorkOrder.ShouldNotBeNull(
			"Expected a work order for lawn care created by tlovejoy and assigned to gwillie");
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
