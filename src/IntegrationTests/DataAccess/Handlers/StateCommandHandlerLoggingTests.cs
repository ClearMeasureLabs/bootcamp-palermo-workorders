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
    public async Task Handle_ShouldLogStructuredExecutingAndExecuted()
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
        var logger = new CapturingLogger();
        var handler = new StateCommandHandler(
            TestHost.GetRequiredService<DbContext>(),
            TestHost.GetRequiredService<TimeProvider>(),
            TestHost.GetRequiredService<IDistributedBus>(),
            logger);

        await handler.Handle(command);

        var info = logger.Entries.Where(e => e.Level == LogLevel.Information).ToList();
        info.Count.ShouldBeGreaterThanOrEqualTo(2);

        var executing = info.First(e => e.Message.StartsWith("Executing"));
        executing.Values["Command"].ShouldBe(nameof(SaveDraftCommand));
        executing.Values["Number"].ShouldBe(expectedNumber);

        var executed = info.First(e => e.Message.StartsWith("Executed"));
        executed.Values["Command"].ShouldBe(nameof(SaveDraftCommand));
        executed.Values["Number"].ShouldBe(expectedNumber);
        executed.Values["Status"].ShouldBe(WorkOrderStatus.Draft);
    }

    private sealed class CapturingLogger : ILogger<StateCommandHandler>
    {
        public List<LogCapture> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var values = new Dictionary<string, object?>();
            if (state is IEnumerable<KeyValuePair<string, object>> pairs)
            {
                foreach (var pair in pairs)
                {
                    values[pair.Key] = pair.Value;
                }
            }

            Entries.Add(new LogCapture(logLevel, formatter(state, exception), values));
        }
    }

    private sealed record LogCapture(
        LogLevel Level,
        string Message,
        Dictionary<string, object?> Values);
}
