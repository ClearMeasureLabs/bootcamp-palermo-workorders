using System.Diagnostics;
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
    [Retry(3)]
    public async Task Handle_AskForWorkOrdersICreated_ReturnsWorkOrderData()
    {
        new ZDataLoader().LoadData();
        var handler = TestHost.GetRequiredService<ApplicationChatHandler>();
        var currentUser = "tlovejoy";
        var query = new ApplicationChatQuery("Show me all the work orders that I created", currentUser);

        ChatResponse response = await ExecuteLlmAsync(() => handler.Handle(query, CancellationToken.None));

        response.ShouldNotBeNull();
        response.Messages.ShouldNotBeEmpty();
        response.Messages.Last().Text.ShouldNotBeNullOrWhiteSpace();
        await TestContext.Out.WriteLineAsync(response.Messages.Last().Text!);
    }

    [Test]
    [Retry(3)]
    public async Task Handle_CreateAndAssignWorkOrder_CreatesAssignedWorkOrderForGwillie()
    {
        new ZDataLoader().LoadData();
        var handler = TestHost.GetRequiredService<ApplicationChatHandler>();
        var query = new ApplicationChatQuery(
            "As tlovejoy, create a work order for mowing grass ",
            "tlovejoy");

        ChatResponse response = await ExecuteLlmAsync(() => handler.Handle(query, CancellationToken.None));

        var responseText = response.Messages.LastOrDefault()?.Text;
        await TestContext.Out.WriteLineAsync($"LLM response: {responseText}");

        var factory = TestHost.GetRequiredService<ChatClientFactory>();
        IChatClient parseClient = await factory.GetChatClient();
        ChatResponse parseResponse = await ExecuteLlmAsync(() => parseClient.GetResponseAsync(
        [
            new(ChatRole.System,
                "Extract only the work order number from the following text. " +
                "Return nothing but the work order number itself, with no extra text."),
            new(ChatRole.User, responseText)
        ]));
        var workOrderNumber = parseResponse.Messages.Last().Text!.Trim();
        await TestContext.Out.WriteLineAsync($"Parsed work order number: {workOrderNumber}");

        var db = TestHost.GetRequiredService<DataContext>();
        var workOrder = await db.Set<WorkOrder>()
            .SingleOrDefaultAsync(wo => wo.Number == workOrderNumber);

        workOrder.ShouldNotBeNull($"No work order found with number '{workOrderNumber}'");
        workOrder.Status.ShouldBe(WorkOrderStatus.Draft);
    }

    [Test]
    [Retry(80)]
    public async Task Handle_CreateAndAssignWorkOrder_AssignsWorkOrderForWilie()
    {
        new ZDataLoader().LoadData();
        var handler = TestHost.GetRequiredService<ApplicationChatHandler>();
        var query = new ApplicationChatQuery(
            "have groundskeeper willie mow the grass. Yes, assign the new work order. confirmed",
            "tlovejoy");

        ChatResponse response = await ExecuteLlmAsync(() => handler.Handle(query, CancellationToken.None));

        var responseText = response.Messages.LastOrDefault()?.Text;
        await TestContext.Out.WriteLineAsync($"LLM response: {responseText}");

        var factory = TestHost.GetRequiredService<ChatClientFactory>();
        IChatClient parseClient = await factory.GetChatClient();
        ChatResponse parseResponse = await ExecuteLlmAsync(() => parseClient.GetResponseAsync(
        [
            new(ChatRole.System,
                "Extract only the work order number from the following text. " +
                "Return nothing but the work order number itself, with no extra text."),
            new(ChatRole.User, responseText)
        ]));
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
    [Retry(80)]
    [Category("SqlServerOnly")]
    public async Task Handle_CreateAndAssignWorkOrder_AssignsWorkOrderForWilieAndThenShelvesIt()
    {
        new ZDataLoader().LoadData();

        var createResponse = await ExecuteAsync(
            "Create a new work order to 'mow the grass', assign it to Groundskeeper Willie, " +
            "only return the work order number");
        var workOrderNumber = await ParseWorkOrderNumberAsync(createResponse);
        await TestContext.Out.WriteLineAsync($"Parsed work order number: {workOrderNumber}");

        await EnsureWorkOrderReachStatusAsync(workOrderNumber, WorkOrderStatus.Assigned, assignRetries: 3);

        await ExecuteAsync($"make work order {workOrderNumber} in progress", "gwillie");

        await EnsureWorkOrderReachStatusAsync(workOrderNumber, WorkOrderStatus.InProgress, assignRetries: 0);

        await ExecuteAsync($"Shelve work order {workOrderNumber}", "gwillie");

        await EnsureWorkOrderReachStatusAsync(workOrderNumber, WorkOrderStatus.Assigned, assignRetries: 0);

        async Task<string> ExecuteAsync(string text, string user = "tlovejoy")
        {
            var handler = TestHost.GetRequiredService<ApplicationChatHandler>();
            var query = new ApplicationChatQuery(text, user);

            ChatResponse response = await ExecuteLlmAsync(() => handler.Handle(query, CancellationToken.None));

            return response.Messages.LastOrDefault()?.Text!;
        }

        async Task EnsureWorkOrderReachStatusAsync(
            string number,
            WorkOrderStatus expectedStatus,
            int assignRetries)
        {
            var assignAttemptsRemaining = assignRetries;
            var deadline = Stopwatch.GetTimestamp() + Stopwatch.Frequency * 90;

            while (Stopwatch.GetTimestamp() < deadline)
            {
                var db = TestHost.GetRequiredService<DataContext>();
                var workOrder = await db.Set<WorkOrder>()
                    .SingleOrDefaultAsync(wo => wo.Number == number);

                workOrder.ShouldNotBeNull($"No work order found with number '{number}'");

                if (workOrder.Status == expectedStatus)
                {
                    workOrder.Assignee?.FirstName.ShouldBe("Groundskeeper Willie");
                    workOrder.Creator?.FirstName.ShouldBe("Timothy");
                    return;
                }

                if (expectedStatus == WorkOrderStatus.Assigned
                    && workOrder.Status == WorkOrderStatus.Draft
                    && assignAttemptsRemaining > 0)
                {
                    assignAttemptsRemaining--;
                    await ExecuteAsync(
                        $"Assign work order {number} to Groundskeeper Willie. confirmed",
                        "tlovejoy");
                }

                await Task.Delay(400);
            }

            var dbFinal = TestHost.GetRequiredService<DataContext>();
            var final = await dbFinal.Set<WorkOrder>().SingleOrDefaultAsync(wo => wo.Number == number);
            final.ShouldNotBeNull();
            final!.Status.ShouldBe(expectedStatus);
        }
    }

    private async Task<string> ParseWorkOrderNumberAsync(string? responseText)
    {
        responseText.ShouldNotBeNullOrWhiteSpace();

        var factory = TestHost.GetRequiredService<ChatClientFactory>();
        var parseClient = await factory.GetChatClient();
        var parseResponse = await ExecuteLlmAsync(() => parseClient.GetResponseAsync(
        [
            new(ChatRole.System,
                "Extract only the work order number from the following text. " +
                "Return nothing but the work order number itself, with no extra text."),
            new(ChatRole.User, responseText)
        ]));

        return parseResponse.Messages.Last().Text!.Trim();
    }
}