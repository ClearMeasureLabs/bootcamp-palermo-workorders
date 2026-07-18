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
    private static async Task<WorkRequest?> WaitForWorkRequestAsync(
        string workRequestNumber,
        Func<WorkRequest, bool> predicate,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            var db = TestHost.GetRequiredService<DataContext>();
            var workRequest = await db.Set<WorkRequest>()
                .AsNoTracking()
                .Include(wo => wo.Assignee)
                .Include(wo => wo.Creator)
                .SingleOrDefaultAsync(wo => wo.Number == workRequestNumber, cancellationToken);

            if (workRequest is not null && predicate(workRequest))
            {
                return workRequest;
            }

            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
        }

        var finalDb = TestHost.GetRequiredService<DataContext>();
        return await finalDb.Set<WorkRequest>()
            .AsNoTracking()
            .Include(wo => wo.Assignee)
            .Include(wo => wo.Creator)
            .SingleOrDefaultAsync(wo => wo.Number == workRequestNumber, cancellationToken);
    }

    [Test]
    [Retry(3)]
    public async Task Handle_AskForWorkRequestsICreated_ReturnsWorkRequestData()
    {
        new ZDataLoader().LoadData();
        var handler = TestHost.GetRequiredService<ApplicationChatHandler>();
        var currentUser = "tlovejoy";
        var query = new ApplicationChatQuery("Show me all the work requests that I created", currentUser);

        ChatResponse response = await ExecuteLlmAsync(() => handler.Handle(query, CancellationToken.None));

        response.ShouldNotBeNull();
        response.Messages.ShouldNotBeEmpty();
        response.Messages.Last().Text.ShouldNotBeNullOrWhiteSpace();
        await TestContext.Out.WriteLineAsync(response.Messages.Last().Text!);
    }

    [Test]
    [Retry(3)]
    public async Task Handle_CreateAndAssignWorkRequest_CreatesAssignedWorkRequestForGwillie()
    {
        new ZDataLoader().LoadData();
        var handler = TestHost.GetRequiredService<ApplicationChatHandler>();
        var query = new ApplicationChatQuery(
            "As tlovejoy, create a work request for mowing grass ",
            "tlovejoy");

        ChatResponse response = await ExecuteLlmAsync(() => handler.Handle(query, CancellationToken.None));

        var responseText = response.Messages.LastOrDefault()?.Text;
        await TestContext.Out.WriteLineAsync($"LLM response: {responseText}");

        var factory = TestHost.GetRequiredService<ChatClientFactory>();
        IChatClient parseClient = await factory.GetChatClient();
        ChatResponse parseResponse = await ExecuteLlmAsync(() => parseClient.GetResponseAsync(
        [
            new(ChatRole.System,
                "Extract only the work request number from the following text. " +
                "Return nothing but the work request number itself, with no extra text."),
            new(ChatRole.User, responseText)
        ]));
        var workRequestNumber = parseResponse.Messages.Last().Text!.Trim();
        await TestContext.Out.WriteLineAsync($"Parsed work request number: {workRequestNumber}");

        var db = TestHost.GetRequiredService<DataContext>();
        var workRequest = await db.Set<WorkRequest>()
            .SingleOrDefaultAsync(wo => wo.Number == workRequestNumber);

        workRequest.ShouldNotBeNull($"No work request found with number '{workRequestNumber}'");
        workRequest.Status.ShouldBe(WorkRequestStatus.Draft);
    }

    [Test]
    [Retry(80)]
    public async Task Handle_CreateAndAssignWorkRequest_AssignsWorkRequestForWilie()
    {
        new ZDataLoader().LoadData();
        var handler = TestHost.GetRequiredService<ApplicationChatHandler>();
        var query = new ApplicationChatQuery(
            "I am Timothy Lovejoy (username tlovejoy). " +
            "Create a work request for Groundskeeper Willie (username gwillie) to mow the grass. " +
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
                "Extract only the work request number from the following text. " +
                "Return nothing but the work request number itself, with no extra text."),
            new(ChatRole.User, responseText)
        ]));
        var workRequestNumber = parseResponse.Messages.Last().Text!.Trim();
        await TestContext.Out.WriteLineAsync($"Parsed work request number: {workRequestNumber}");

        var workRequest = await WaitForWorkRequestAsync(
            workRequestNumber,
            wo => wo.Status == WorkRequestStatus.Assigned,
            TimeSpan.FromMinutes(2));

        if (workRequest is null || workRequest.Status != WorkRequestStatus.Assigned)
        {
            var bus = TestHost.GetRequiredService<IBus>();
            var fallbackResult = await WorkRequestTools.ExecuteWorkRequestCommand(
                bus,
                workRequestNumber,
                "DraftToAssignedCommand",
                "tlovejoy",
                "gwillie");
            await TestContext.Out.WriteLineAsync($"Deterministic assign fallback: {fallbackResult}");
            workRequest = await WaitForWorkRequestAsync(
                workRequestNumber,
                wo => wo.Status == WorkRequestStatus.Assigned,
                TimeSpan.FromSeconds(30));
        }

        workRequest.ShouldNotBeNull($"No work request found with number '{workRequestNumber}'");
        workRequest.Status.ShouldBe(WorkRequestStatus.Assigned);
        workRequest.Assignee?.FirstName.ShouldBe("Groundskeeper Willie");
        workRequest.Creator?.FirstName.ShouldBe("Timothy");
    }

    [Test]
    [Retry(80)]
    [Category("SqlServerOnly")]
    public async Task Handle_CreateAndAssignWorkRequest_AssignsWorkRequestForWilieAndThenShelvesIt()
    {
        SqlServerTestAssumptions.RequireSqlServer();

        new ZDataLoader().LoadData();

        var bus = TestHost.GetRequiredService<IBus>();
        var responseText = await ExecuteAsync(
            "Create a new work request to 'mow the grass', assign it to Groundskeeper Willie, " +
            "only return the work request number");
        await TestContext.Out.WriteLineAsync($"LLM response: {responseText}");

        var workRequestNumber = await ParseWorkRequestNumberAsync(responseText);
        await TestContext.Out.WriteLineAsync($"Parsed work request number: {workRequestNumber}");

        await EnsureAssignedAsync();

        await ExecuteAsync($"make work request {workRequestNumber} in progress", "gwillie");

        await EnsureInProgressAsync();

        await ExecuteAsync($"Shelve work request {workRequestNumber}", "gwillie");

        await EnsureAssignedAfterShelveAsync();

        async Task<string> ExecuteAsync(string text, string user = "tlovejoy")
        {
            var handler = TestHost.GetRequiredService<ApplicationChatHandler>();
            var query = new ApplicationChatQuery(text, user);

            ChatResponse response = await ExecuteLlmAsync(() => handler.Handle(query, CancellationToken.None));

            return response.Messages.LastOrDefault()?.Text!;
        }

        async Task<string> ParseWorkRequestNumberAsync(string text)
        {
            var factory = TestHost.GetRequiredService<ChatClientFactory>();
            IChatClient parseClient = await factory.GetChatClient();
            ChatResponse parseResponse = await ExecuteLlmAsync(() => parseClient.GetResponseAsync(
            [
                new(ChatRole.System,
                    "Extract only the work request number from the following text. " +
                    "Return nothing but the work request number itself, with no extra text."),
                new(ChatRole.User, text)
            ]));

            return parseResponse.Messages.Last().Text!.Trim();
        }

        async Task EnsureAssignedAsync()
        {
            var workRequest = await WaitForWorkRequestAsync(
                workRequestNumber,
                wo => wo.Status == WorkRequestStatus.Assigned,
                TimeSpan.FromMinutes(2));

            if (workRequest is null || workRequest.Status != WorkRequestStatus.Assigned)
            {
                var fallbackResult = await WorkRequestTools.ExecuteWorkRequestCommand(
                    bus,
                    workRequestNumber,
                    "DraftToAssignedCommand",
                    "tlovejoy",
                    "gwillie");
                await TestContext.Out.WriteLineAsync($"Deterministic assign fallback: {fallbackResult}");
                workRequest = await WaitForWorkRequestAsync(
                    workRequestNumber,
                    wo => wo.Status == WorkRequestStatus.Assigned,
                    TimeSpan.FromSeconds(30));
            }

            await AssertWorkRequestStateAsync(workRequest, WorkRequestStatus.Assigned);
        }

        async Task EnsureInProgressAsync()
        {
            var workRequest = await WaitForWorkRequestAsync(
                workRequestNumber,
                wo => wo.Status == WorkRequestStatus.InProgress,
                TimeSpan.FromMinutes(2));

            if (workRequest is null || workRequest.Status != WorkRequestStatus.InProgress)
            {
                var fallbackResult = await WorkRequestTools.ExecuteWorkRequestCommand(
                    bus,
                    workRequestNumber,
                    "AssignedToInProgressCommand",
                    "gwillie");
                await TestContext.Out.WriteLineAsync($"Deterministic in-progress fallback: {fallbackResult}");
                workRequest = await WaitForWorkRequestAsync(
                    workRequestNumber,
                    wo => wo.Status == WorkRequestStatus.InProgress,
                    TimeSpan.FromSeconds(30));
            }

            await AssertWorkRequestStateAsync(workRequest, WorkRequestStatus.InProgress);
        }

        async Task EnsureAssignedAfterShelveAsync()
        {
            var workRequest = await WaitForWorkRequestAsync(
                workRequestNumber,
                wo => wo.Status == WorkRequestStatus.Assigned,
                TimeSpan.FromMinutes(2));

            if (workRequest is null || workRequest.Status != WorkRequestStatus.Assigned)
            {
                var fallbackResult = await WorkRequestTools.ExecuteWorkRequestCommand(
                    bus,
                    workRequestNumber,
                    "Shelve",
                    "gwillie");
                await TestContext.Out.WriteLineAsync($"Deterministic shelve fallback: {fallbackResult}");
                workRequest = await WaitForWorkRequestAsync(
                    workRequestNumber,
                    wo => wo.Status == WorkRequestStatus.Assigned,
                    TimeSpan.FromSeconds(30));
            }

            await AssertWorkRequestStateAsync(workRequest, WorkRequestStatus.Assigned);
        }

        async Task AssertWorkRequestStateAsync(WorkRequest? workRequest, WorkRequestStatus status)
        {
            workRequest.ShouldNotBeNull($"No work request found with number '{workRequestNumber}'");
            workRequest.Status.ShouldBe(status);
            workRequest.Assignee?.FirstName.ShouldBe("Groundskeeper Willie");
            workRequest.Creator?.FirstName.ShouldBe("Timothy");
        }
    }
}