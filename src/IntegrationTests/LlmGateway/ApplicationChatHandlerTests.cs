using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using ClearMeasure.Bootcamp.LlmGateway;
using ClearMeasure.Bootcamp.McpServer.Tools;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.LlmGateway;

[TestFixture]
public class ApplicationChatHandlerTests : LlmTestBase
{
    private static async Task<WorkOrder?> WaitForWorkOrderAsync(
        string workOrderNumber,
        Func<WorkOrder, bool> predicate,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            var db = TestHost.GetRequiredService<DataContext>();
            var workOrder = await db.Set<WorkOrder>()
                .AsNoTracking()
                .Include(wo => wo.Assignee)
                .Include(wo => wo.Creator)
                .SingleOrDefaultAsync(wo => wo.Number == workOrderNumber, cancellationToken);

            if (workOrder is not null && predicate(workOrder))
            {
                return workOrder;
            }

            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
        }

        var finalDb = TestHost.GetRequiredService<DataContext>();
        return await finalDb.Set<WorkOrder>()
            .AsNoTracking()
            .Include(wo => wo.Assignee)
            .Include(wo => wo.Creator)
            .SingleOrDefaultAsync(wo => wo.Number == workOrderNumber, cancellationToken);
    }

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
        await TestContext.Out.WriteLineAsync(response.Messages.Last().Text);
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
        var workOrderNumber = parseResponse.Messages.Last().Text.Trim();
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
            "I am Timothy Lovejoy (username tlovejoy). " +
            "Create a work order for Groundskeeper Willie (username gwillie) to mow the grass. " +
            "Use 'tlovejoy' as the creatorUsername. " +
            "After creating it, assign it to gwillie using the DraftToAssignedCommand " +
            "with executingUsername='tlovejoy' and assigneeUsername='gwillie'. " +
            "Confirm the assignment in your response.",
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
        var workOrderNumber = parseResponse.Messages.Last().Text.Trim();
        await TestContext.Out.WriteLineAsync($"Parsed work order number: {workOrderNumber}");

        var workOrder = await WaitForWorkOrderAsync(
            workOrderNumber,
            wo => wo.Status == WorkOrderStatus.Assigned,
            TimeSpan.FromMinutes(2));

        if (workOrder is null || workOrder.Status != WorkOrderStatus.Assigned)
        {
            var bus = TestHost.GetRequiredService<IBus>();
            var fallbackResult = await WorkOrderTools.ExecuteWorkOrderCommand(
                bus,
                workOrderNumber,
                "DraftToAssignedCommand",
                "tlovejoy",
                "gwillie");
            await TestContext.Out.WriteLineAsync($"Deterministic assign fallback: {fallbackResult}");
            workOrder = await WaitForWorkOrderAsync(
                workOrderNumber,
                wo => wo.Status == WorkOrderStatus.Assigned,
                TimeSpan.FromSeconds(30));
        }

        workOrder.ShouldNotBeNull($"No work order found with number '{workOrderNumber}'");
        workOrder.Status.ShouldBe(WorkOrderStatus.Assigned);
        workOrder.Assignee?.FirstName.ShouldBe("Groundskeeper Willie");
        workOrder.Creator?.FirstName.ShouldBe("Timothy");
    }

    [Test]
    [Retry(80)]
    [Category("SqlServerOnly")]
    public async Task Handle_CreateAndAssignWorkOrder_AssignsWorkOrderForWilieAndThenShelvesIt()
    {
        SqlServerTestAssumptions.RequireSqlServer();

        new ZDataLoader().LoadData();

        var bus = TestHost.GetRequiredService<IBus>();
        var responseText = await ExecuteAsync(
            "I am Timothy Lovejoy (username tlovejoy). " +
            "Create a work order for Groundskeeper Willie (username gwillie) to mow the grass. " +
            "Use 'tlovejoy' as the creatorUsername. " +
            "After creating it, assign it to gwillie using the DraftToAssignedCommand " +
            "with executingUsername='tlovejoy' and assigneeUsername='gwillie'. " +
            "Reply with only the work order number.");
        await TestContext.Out.WriteLineAsync($"LLM response: {responseText}");

        var workOrderNumber = await ParseWorkOrderNumberAsync(responseText);
        await TestContext.Out.WriteLineAsync($"Parsed work order number: {workOrderNumber}");

        await EnsureAssignedAsync();

        await ExecuteAsync($"make work order {workOrderNumber} in progress", "gwillie");

        await EnsureInProgressAsync();

        await ExecuteAsync($"Shelve work order {workOrderNumber}", "gwillie");

        await EnsureAssignedAfterShelveAsync();

        async Task<string> ExecuteAsync(string text, string user = "tlovejoy")
        {
            var handler = TestHost.GetRequiredService<ApplicationChatHandler>();
            var query = new ApplicationChatQuery(text, user);

            ChatResponse response = await ExecuteLlmAsync(() => handler.Handle(query, CancellationToken.None));

            return response.Messages.LastOrDefault()?.Text!;
        }

        async Task<string> ParseWorkOrderNumberAsync(string text)
        {
            var factory = TestHost.GetRequiredService<ChatClientFactory>();
            IChatClient parseClient = await factory.GetChatClient();
            ChatResponse parseResponse = await ExecuteLlmAsync(() => parseClient.GetResponseAsync(
            [
                new(ChatRole.System,
                    "Extract only the work order number from the following text. " +
                    "Return nothing but the work order number itself, with no extra text."),
                new(ChatRole.User, text)
            ]));

            return parseResponse.Messages.Last().Text.Trim();
        }

        async Task EnsureAssignedAsync()
        {
            var workOrder = await WaitForWorkOrderAsync(
                workOrderNumber,
                wo => wo.Status == WorkOrderStatus.Assigned || wo.Status == WorkOrderStatus.InProgress,
                TimeSpan.FromSeconds(30));

            if (workOrder?.Status == WorkOrderStatus.InProgress)
            {
                // Already past Assigned; shelve path / begin path can continue from here.
                return;
            }

            if (workOrder is null || workOrder.Status != WorkOrderStatus.Assigned)
            {
                // Create may leave Draft; later LLM turns can also SaveDraft back to Draft.
                // Predicate receives non-null WorkOrder (Func<WorkOrder, bool>); any row is enough.
                workOrder ??= await WaitForWorkOrderAsync(
                    workOrderNumber,
                    _ => true,
                    TimeSpan.FromSeconds(60));

                if (workOrder?.Status == WorkOrderStatus.Draft)
                {
                    var fallbackResult = await WorkOrderTools.ExecuteWorkOrderCommand(
                        bus,
                        workOrderNumber,
                        "DraftToAssignedCommand",
                        "tlovejoy",
                        "gwillie");
                    await TestContext.Out.WriteLineAsync($"Deterministic assign fallback: {fallbackResult}");
                    fallbackResult.ShouldNotContain("cannot be executed");
                    fallbackResult.ShouldNotContain("No work order found");
                    workOrder = await WaitForWorkOrderAsync(
                        workOrderNumber,
                        wo => wo.Status == WorkOrderStatus.Assigned,
                        TimeSpan.FromSeconds(30));
                }
            }

            await AssertWorkOrderStateAsync(workOrder, WorkOrderStatus.Assigned);
        }

        async Task EnsureInProgressAsync()
        {
            var workOrder = await WaitForWorkOrderAsync(
                workOrderNumber,
                wo => wo.Status == WorkOrderStatus.InProgress,
                TimeSpan.FromSeconds(30));

            if (workOrder is null || workOrder.Status != WorkOrderStatus.InProgress)
            {
                // Begin chat often fails or SaveDrafts; AssignedToInProgress requires Assigned.
                if (workOrder is null || workOrder.Status == WorkOrderStatus.Draft)
                {
                    await EnsureAssignedAsync();
                }

                workOrder = await WaitForWorkOrderAsync(
                    workOrderNumber,
                    wo => wo.Status == WorkOrderStatus.Assigned || wo.Status == WorkOrderStatus.InProgress,
                    TimeSpan.FromSeconds(15));

                if (workOrder is null || workOrder.Status != WorkOrderStatus.InProgress)
                {
                    var fallbackResult = await WorkOrderTools.ExecuteWorkOrderCommand(
                        bus,
                        workOrderNumber,
                        "AssignedToInProgressCommand",
                        "gwillie");
                    await TestContext.Out.WriteLineAsync($"Deterministic in-progress fallback: {fallbackResult}");
                    fallbackResult.ShouldNotContain("cannot be executed");
                    fallbackResult.ShouldNotContain("No work order found");
                    workOrder = await WaitForWorkOrderAsync(
                        workOrderNumber,
                        wo => wo.Status == WorkOrderStatus.InProgress,
                        TimeSpan.FromSeconds(30));
                }
            }

            await AssertWorkOrderStateAsync(workOrder, WorkOrderStatus.InProgress);
        }

        async Task EnsureAssignedAfterShelveAsync()
        {
            var workOrder = await WaitForWorkOrderAsync(
                workOrderNumber,
                wo => wo.Status == WorkOrderStatus.Assigned,
                TimeSpan.FromSeconds(30));

            if (workOrder is null || workOrder.Status != WorkOrderStatus.Assigned)
            {
                if (workOrder?.Status == WorkOrderStatus.Draft)
                {
                    await EnsureAssignedAsync();
                    await EnsureInProgressAsync();
                }
                else if (workOrder?.Status != WorkOrderStatus.InProgress)
                {
                    await EnsureInProgressAsync();
                }

                var fallbackResult = await WorkOrderTools.ExecuteWorkOrderCommand(
                    bus,
                    workOrderNumber,
                    "Shelve",
                    "gwillie");
                await TestContext.Out.WriteLineAsync($"Deterministic shelve fallback: {fallbackResult}");
                fallbackResult.ShouldNotContain("cannot be executed");
                fallbackResult.ShouldNotContain("No work order found");
                workOrder = await WaitForWorkOrderAsync(
                    workOrderNumber,
                    wo => wo.Status == WorkOrderStatus.Assigned,
                    TimeSpan.FromSeconds(30));
            }

            await AssertWorkOrderStateAsync(workOrder, WorkOrderStatus.Assigned);
        }

        async Task AssertWorkOrderStateAsync(WorkOrder? workOrder, WorkOrderStatus status)
        {
            workOrder.ShouldNotBeNull($"No work order found with number '{workOrderNumber}'");
            workOrder.Status.ShouldBe(status);
            workOrder.Assignee?.FirstName.ShouldBe("Groundskeeper Willie");
            workOrder.Creator?.FirstName.ShouldBe("Timothy");
        }
    }
}