using ClearMeasure.Bootcamp.Core.Model.Events;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using Shouldly;
using System.Diagnostics.Metrics;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess.Handlers;

[TestFixture]
public class TelemetryHandlerTests
{
    [Test]
    public async Task ShouldIncrementLoginCounter_WhenUserLogsIn()
    {
        long recorded = 0;
        string? recordedUserName = null;

        using var listener = new MeterListener();

        listener.InstrumentPublished = (instrument, meterListener) =>
        {
            if (instrument.Name == "app.user.logins")
            {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };

        listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, state) =>
        {
            recorded += measurement;

            foreach (var tag in tags)
            {
                if (tag.Key == "user.name")
                {
                    recordedUserName = tag.Value?.ToString();
                }
            }
        });

        listener.Start();

        var handler = TestHost.GetRequiredService<TelemetryHandler>();
        var loginEvent = new UserLoggedInEvent("testuser");

        await handler.Handle(loginEvent, CancellationToken.None);

        listener.RecordObservableInstruments();

        recorded.ShouldBeGreaterThan(0);
        recordedUserName.ShouldBe("testuser");
    }

    [Test]
    public async Task Handle_WithMultipleLogins_IncrementsCounterForEach()
    {
        long recorded = 0;

        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, meterListener) =>
        {
            if (instrument.Name == "app.user.logins")
            {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };

        listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, state) =>
        {
            recorded += measurement;
        });

        listener.Start();

        var handler = TestHost.GetRequiredService<TelemetryHandler>();

        await handler.Handle(new UserLoggedInEvent("user1"), CancellationToken.None);
        await handler.Handle(new UserLoggedInEvent("user2"), CancellationToken.None);

        listener.RecordObservableInstruments();

        recorded.ShouldBe(2);
    }
}
