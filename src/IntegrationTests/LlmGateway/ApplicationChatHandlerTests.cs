using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using ClearMeasure.Bootcamp.LlmGateway;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.LlmGateway;

[TestFixture]
public class ApplicationChatHandlerTests : LlmTestBase
{
    [Test]
    public async Task Handle_AskForWorkOrdersICreated_ReturnsWorkOrderData()
    {
        new ZDataLoader().LoadData();
        var handler = TestHost.GetRequiredService<ApplicationChatHandler>();
        var currentUser = "tlovejoy";
        var query = new ApplicationChatQuery("Show me all the work orders that I created", currentUser);

        ChatResponse response = await handler.Handle(query, CancellationToken.None);

        response.ShouldNotBeNull();
        response.Messages.ShouldNotBeEmpty();
        response.Messages.Last().Text.ShouldNotBeNullOrWhiteSpace();
        await TestContext.Out.WriteLineAsync(response.Messages.Last().Text!);
    }

    [Test]
    public async Task Handle_CreateAndAssignWorkOrder_CreatesAssignedWorkOrderForGwillie()
    {
        new ZDataLoader().LoadData();
        var handler = TestHost.GetRequiredService<ApplicationChatHandler>();
        var query = new ApplicationChatQuery(
            "As tlovejoy, create a work order for mowing grass ",
            "tlovejoy");

        ChatResponse response = await handler.Handle(query, CancellationToken.None);

        var responseText = response.Messages.LastOrDefault()?.Text;
        await TestContext.Out.WriteLineAsync($"LLM response: {responseText}");

        var factory = TestHost.GetRequiredService<ChatClientFactory>();
        IChatClient parseClient = await factory.GetChatClient();
        ChatResponse parseResponse = await parseClient.GetResponseAsync(
        [
            new(ChatRole.System,
                "Extract only the work order number from the following text. " +
                "Return nothing but the work order number itself, with no extra text."),
            new(ChatRole.User, responseText)
        ]);
        var workOrderNumber = parseResponse.Messages.Last().Text!.Trim();
        await TestContext.Out.WriteLineAsync($"Parsed work order number: {workOrderNumber}");

        var db = TestHost.GetRequiredService<DataContext>();
        var workOrder = await db.Set<WorkOrder>()
            .SingleOrDefaultAsync(wo => wo.Number == workOrderNumber);

        workOrder.ShouldNotBeNull($"No work order found with number '{workOrderNumber}'");
        workOrder.Status.ShouldBe(WorkOrderStatus.Draft);
    }

    [Test]
    public async Task Handle_CreateAndAssignWorkOrder_AssignsWorkOrderForWilie()
    {
        new ZDataLoader().LoadData();
        var handler = TestHost.GetRequiredService<ApplicationChatHandler>();
        var query = new ApplicationChatQuery(
            "have groundskeeper willie mow the grass. Yes, assign the new work order. confirmed",
            "tlovejoy");

        ChatResponse response = await handler.Handle(query, CancellationToken.None);

        var responseText = response.Messages.LastOrDefault()?.Text;
        await TestContext.Out.WriteLineAsync($"LLM response: {responseText}");

        var factory = TestHost.GetRequiredService<ChatClientFactory>();
        IChatClient parseClient = await factory.GetChatClient();
        ChatResponse parseResponse = await parseClient.GetResponseAsync(
        [
            new(ChatRole.System,
                "Extract only the work order number from the following text. " +
                "Return nothing but the work order number itself, with no extra text."),
            new(ChatRole.User, responseText)
        ]);
        var workOrderNumber = parseResponse.Messages.Last().Text!.Trim();
        await TestContext.Out.WriteLineAsync($"Parsed work order number: {workOrderNumber}");

        var db = TestHost.GetRequiredService<DataContext>();
        var workOrder = await db.Set<WorkOrder>()
            .SingleOrDefaultAsync(wo => wo.Number == workOrderNumber);

        workOrder.ShouldNotBeNull($"No work order found with number '{workOrderNumber}'");
        workOrder.Status.ShouldBe(WorkOrderStatus.Assigned);
        workOrder?.Assignee?.FirstName.ShouldBe("Groundskeeper Willie");
        workOrder?.Creator?.FirstName.ShouldBe("Timothy");
    }

    [Test]
    public async Task Handle_CreateAndAssignWorkOrder_AssignsWorkOrderForWilieAndThenShelvesIt()
    {
        new ZDataLoader().LoadData();

        var workOrderNumber = await ExecuteAsync(
            "Create a new work order to 'mow the grass', assign it to Groundskeeper Willie, " +
            "only return the work order number");

        await CheckStatusAsync(WorkOrderStatus.Assigned);

        await ExecuteAsync($"make work order {workOrderNumber} in progress", "gwillie");

        await CheckStatusAsync(WorkOrderStatus.InProgress);

        await ExecuteAsync($"Shelve work order {workOrderNumber}", "gwillie");

        await CheckStatusAsync(WorkOrderStatus.Assigned);

        async Task<string> ExecuteAsync(string text, string user = "tlovejoy")
        {
            var handler = TestHost.GetRequiredService<ApplicationChatHandler>();
            var query = new ApplicationChatQuery(text, user);

            ChatResponse response = await handler.Handle(query, CancellationToken.None);

            return response.Messages.LastOrDefault()?.Text!;
        }

        async Task CheckStatusAsync(WorkOrderStatus status)
        {
            var db = TestHost.GetRequiredService<DataContext>();
            var workOrder = await db.Set<WorkOrder>()
                .SingleOrDefaultAsync(wo => wo.Number == workOrderNumber);

            workOrder.ShouldNotBeNull($"No work order found with number '{workOrderNumber}'");
            workOrder.Status.ShouldBe(status);
            workOrder?.Assignee?.FirstName.ShouldBe("Groundskeeper Willie");
            workOrder?.Creator?.FirstName.ShouldBe("Timothy");
        }
    }
}