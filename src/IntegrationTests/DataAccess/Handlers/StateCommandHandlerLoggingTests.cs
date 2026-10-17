using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.UnitTests.Core.Queries;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess.Handlers;

public class StateCommandHandlerLoggingTests : IntegratedTestBase
{
    [Test]
    public async Task Handle_WhenSavingDraft_LogsStructuredCommandNumberAndStatus()
    {
        new DatabaseTests().Clean();

        var currentUser = Faker<Employee>();
        currentUser.Id = Guid.NewGuid();
        var context = TestHost.GetRequiredService<DbContext>();
        context.Add(currentUser);
        await context.SaveChangesAsync();

        var workOrder = Faker<WorkOrder>();
        workOrder.Id = Guid.Empty;
        workOrder.CreatedDate = null;
        workOrder.Creator = currentUser;
        var expectedNumber = workOrder.Number;

        var command = RemotableRequestTests.SimulateRemoteObject(new SaveDraftCommand(workOrder, currentUser));
        var logger = new CapturingLogger<StateCommandHandler>();
        var handler = new StateCommandHandler(
            TestHost.GetRequiredService<DbContext>(),
            TestHost.GetRequiredService<TimeProvider>(),
            TestHost.GetRequiredService<IDistributedBus>(),
            logger);

        _ = await handler.Handle(command);

        var infoLogs = logger.Entries.Where(entry => entry.Level == LogLevel.Information).ToList();
        infoLogs.Count.ShouldBe(2);

        AssertStructured(infoLogs[0], "Executing {Command} for work order {Number}",
            ("Command", nameof(SaveDraftCommand)),
            ("Number", expectedNumber));
        AssertStructured(infoLogs[1], "Executed {Command} for work order {Number} -> {Status}",
            ("Command", nameof(SaveDraftCommand)),
            ("Number", expectedNumber),
            ("Status", WorkOrderStatus.Draft));
    }

    private static void AssertStructured(
        CapturingLogger<StateCommandHandler>.LogEntry entry,
        string expectedTemplate,
        params (string Key, object? Value)[] expected)
    {
        entry.MessageTemplate.ShouldBe(expectedTemplate);
        foreach (var (key, value) in expected)
        {
            entry.Properties.ShouldContain(pair =>
                pair.Key == key && Equals(pair.Value, value));
        }
    }

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<LogEntry> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var properties = state as IReadOnlyList<KeyValuePair<string, object?>>
                             ?? [];
            var template = properties
                .FirstOrDefault(pair => pair.Key == "{OriginalFormat}")
                .Value?.ToString() ?? formatter(state, exception);
            Entries.Add(new LogEntry(logLevel, template, properties.ToList()));
        }

        public sealed record LogEntry(
            LogLevel Level,
            string MessageTemplate,
            List<KeyValuePair<string, object?>> Properties);
    }
}
