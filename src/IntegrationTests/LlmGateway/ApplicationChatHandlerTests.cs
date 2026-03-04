using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using ClearMeasure.Bootcamp.LlmGateway;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
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

        var workOrder = await WaitForWorkOrderAsync(workOrderNumber, null);

        workOrder.ShouldNotBeNull($"No work order found with number '{workOrderNumber}'");
        workOrder.Status.ShouldBeOneOf(WorkOrderStatus.Draft, WorkOrderStatus.Assigned);
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

        var workOrder = await WaitForWorkOrderAsync(workOrderNumber, WorkOrderStatus.Assigned);

        if (workOrder?.Status != WorkOrderStatus.Assigned)
        {
            var followUpQuery = new ApplicationChatQuery(
                $"Assign work order {workOrderNumber} to Groundskeeper Willie.",
                "tlovejoy");
            await handler.Handle(followUpQuery, CancellationToken.None);
            workOrder = await WaitForWorkOrderAsync(workOrderNumber, WorkOrderStatus.Assigned);
        }

        if (workOrder?.Status != WorkOrderStatus.Assigned)
        {
            Assert.Inconclusive(
                $"LLM did not assign work order '{workOrderNumber}' in time. Final status was '{workOrder?.Status?.FriendlyName}'.");
        }

        workOrder.ShouldNotBeNull($"No work order found with number '{workOrderNumber}'");
        workOrder.Status.ShouldBe(WorkOrderStatus.Assigned);
        workOrder?.Assignee?.FirstName.ShouldBe("Groundskeeper Willie");
        workOrder?.Creator?.FirstName.ShouldBe("Timothy");
    }

    private async Task<WorkOrder?> WaitForWorkOrderAsync(string workOrderNumber, WorkOrderStatus? targetStatus)
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            using var scope = TestHost.GetRequiredService<IServiceScopeFactory>().CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DataContext>();
            var workOrder = await db.Set<WorkOrder>()
                .AsNoTracking()
                .SingleOrDefaultAsync(wo => wo.Number == workOrderNumber);

            if (workOrder != null && (targetStatus == null || workOrder.Status == targetStatus))
            {
                return workOrder;
            }

            await Task.Delay(250);
        }

        using var finalScope = TestHost.GetRequiredService<IServiceScopeFactory>().CreateScope();
        var finalDb = finalScope.ServiceProvider.GetRequiredService<DataContext>();
        return await finalDb.Set<WorkOrder>()
            .AsNoTracking()
            .SingleOrDefaultAsync(wo => wo.Number == workOrderNumber);
    }

    [Test]
    public async Task Handle_CreateAndAssignWorkOrder_AssignsWorkOrderForWilieAndThenShelvesIt()
    {
        new ZDataLoader().LoadData();

        var workOrderNumber = await ExecuteAsync(
            "Create a new work order to 'mow the grass', assign it to Groundskeeper Willie, " +
            "only return the work order number");

        var current = await WaitForWorkOrderAsync(workOrderNumber, null);
        if (current?.Status == WorkOrderStatus.Draft)
        {
            await ExecuteAsync($"Assign work order {workOrderNumber} to Groundskeeper Willie.");
        }

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
            var workOrder = await WaitForWorkOrderAsync(workOrderNumber, status);

            workOrder.ShouldNotBeNull($"No work order found with number '{workOrderNumber}'");
            workOrder.Status.ShouldBe(status);
            workOrder?.Assignee?.FirstName.ShouldBe("Groundskeeper Willie");
            workOrder?.Creator?.FirstName.ShouldBe("Timothy");
        }
    }
}
