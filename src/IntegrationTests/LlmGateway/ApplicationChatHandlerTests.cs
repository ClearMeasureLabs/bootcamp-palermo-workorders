using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using ClearMeasure.Bootcamp.LlmGateway;
using Microsoft.Data.SqlClient;
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
    public async Task Handle_CreateAndAssignWorkOrder_AssignsWorkOrderForWilie_AssignShelve()
    {
        new ZDataLoader().LoadData();
        var handler = TestHost.GetRequiredService<ApplicationChatHandler>();
        var query = new ApplicationChatQuery(
            "create a work order to mow then assign to it to gwillie",
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

        var handler2 = TestHost.GetRequiredService<ApplicationChatHandler>();
        var query2 = new ApplicationChatQuery(
            $"please mark work order {workOrderNumber} as in progress",
            "gwillie");

        ChatResponse response2 = await handler2.Handle(query2, CancellationToken.None);

        var handler3 = TestHost.GetRequiredService<ApplicationChatHandler>();
        var query3 = new ApplicationChatQuery(
            $"Please shelve the work order {workOrderNumber}",
            "gwillie");

        ChatResponse response3 = await handler3.Handle(query3, CancellationToken.None);

        var responseText3 = response3.Messages.LastOrDefault()?.Text;
        await TestContext.Out.WriteLineAsync($"LLM response: {responseText3}");

        var factory3 = TestHost.GetRequiredService<ChatClientFactory>();
        IChatClient parseClient3 = await factory3.GetChatClient();

        var db2 = TestHost.GetRequiredService<DataContext>();
        var workOrder2 = await db2.Set<WorkOrder>()
            .SingleOrDefaultAsync(wo => wo.Number == workOrderNumber);

        workOrder2.ShouldNotBeNull($"No work order found with number '{workOrderNumber}'");
        workOrder2.Status.ShouldBe(WorkOrderStatus.Assigned);
        workOrder2?.Assignee?.FirstName.ShouldBe("Groundskeeper Willie");
        workOrder2?.Creator?.FirstName.ShouldBe("Timothy");

    }

}