using ClearMeasure.Bootcamp.Core.Model.Events;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using Microsoft.Extensions.Logging;
using Shouldly;
using System.Diagnostics.Metrics;

namespace ClearMeasure.Bootcamp.UnitTests.DataAccess.Handlers;

[TestFixture]
public class DatedWorkOrdersCreatedTelemetryHandlerTests
{
    [Test]
    public async Task ShouldIncrementCounterByEventCount_WhenDatedBatchCreated()
    {
        long recorded = 0;
        const int count = 5;

        using var listener = new MeterListener();
        listener.InstrumentPublished = (publishedInstrument, meterListener) =>
        {
            if (publishedInstrument.Name == "app.workorders.dated_batch_created")
            {
                meterListener.EnableMeasurementEvents(publishedInstrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((_, measurement, _, _) =>
        {
            recorded += measurement;
        });
        listener.Start();

        var logger = new CapturingLogger();
        var handler = new DatedWorkOrdersCreatedTelemetryHandler(logger);
        await handler.Handle(new DatedWorkOrdersCreatedEvent(count), CancellationToken.None);

        listener.RecordObservableInstruments();

        recorded.ShouldBe(count);
    }

    [Test]
    public async Task ShouldLogInformationContainingCount_WhenDatedBatchCreated()
    {
        const int count = 7;
        var logger = new CapturingLogger();
        var handler = new DatedWorkOrdersCreatedTelemetryHandler(logger);

        await handler.Handle(new DatedWorkOrdersCreatedEvent(count), CancellationToken.None);

        logger.Entries.Count.ShouldBe(1);
        logger.Entries[0].Level.ShouldBe(LogLevel.Information);
        logger.Entries[0].Message.ShouldContain(count.ToString());
    }

    private sealed class CapturingLogger : ILogger<DatedWorkOrdersCreatedTelemetryHandler>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Entries.Add((logLevel, formatter(state, exception)));
        }
    }
}
