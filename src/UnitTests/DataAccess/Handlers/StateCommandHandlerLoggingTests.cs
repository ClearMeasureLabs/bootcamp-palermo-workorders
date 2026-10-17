using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.DataAccess.Handlers;

[TestFixture]
public class StateCommandHandlerLoggingTests
{
    private DataContext _context = null!;
    private string _dbPath = null!;

    [SetUp]
    public void SetUp()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"sch_log_test_{Guid.NewGuid():N}.db");
        _context = new DataContext(new FileDbConfig($"Data Source={_dbPath}"));
        _context.Database.EnsureCreated();
    }

    [TearDown]
    public async Task TearDown()
    {
        await _context.DisposeAsync();
        SqliteConnection.ClearAllPools();
        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
    }

    [Test]
    public async Task ShouldLogStructuredCommandAndStatus_WhenExecutingStateCommand()
    {
        var creator = new Employee("logger", "Log", "User", "log@test.com");
        _context.Add(creator);
        await _context.SaveChangesAsync();

        var workOrder = new WorkOrder
        {
            Id = Guid.Empty,
            Number = "WO-LOG-1",
            Title = "Logging test",
            Creator = creator,
            Status = WorkOrderStatus.Draft
        };

        var logger = new FakeLogger<StateCommandHandler>();
        var handler = new StateCommandHandler(
            _context,
            TimeProvider.System,
            new StubDistributedBus(),
            logger);

        await handler.Handle(new SaveDraftCommand(workOrder, creator));

        var infoEntries = logger.Entries.Where(e => e.Level == LogLevel.Information).ToList();
        infoEntries.Count.ShouldBe(2);

        var executing = infoEntries[0];
        executing.Message.ShouldContain("Executing");
        executing.Values["Command"].ShouldBe(nameof(SaveDraftCommand));
        executing.Values["Number"].ShouldBe("WO-LOG-1");

        var executed = infoEntries[1];
        executed.Message.ShouldContain("Executed");
        executed.Values["Command"].ShouldBe(nameof(SaveDraftCommand));
        executed.Values["Number"].ShouldBe("WO-LOG-1");
        executed.Values["Status"].ShouldBe(WorkOrderStatus.Draft);
    }

    private sealed class FileDbConfig(string connectionString) : IDatabaseConfiguration
    {
        public string GetConnectionString() => connectionString;

        public void ResetConnectionPool() { }
    }

    private sealed class StubDistributedBus : IDistributedBus
    {
        public Task PublishAsync<TEvent>(TEvent? @event, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class FakeLogger<T> : ILogger<T>
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
            var values = new Dictionary<string, object?>();
            if (state is IEnumerable<KeyValuePair<string, object?>> pairs)
            {
                foreach (var pair in pairs)
                {
                    values[pair.Key] = pair.Value;
                }
            }

            Entries.Add(new LogEntry(logLevel, formatter(state, exception), values));
        }
    }

    private sealed record LogEntry(LogLevel Level, string Message, Dictionary<string, object?> Values);
}
