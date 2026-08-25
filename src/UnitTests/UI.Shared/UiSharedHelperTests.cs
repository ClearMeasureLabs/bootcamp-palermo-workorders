// ReSharper disable UnusedAutoPropertyAccessor.Local
using System.Diagnostics;
using ClearMeasure.Bootcamp.UI.Shared;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared;

[TestFixture]
public class BusActivityTaggerTests
{
    [Test]
    public void ShouldTagScalarProperties_WhenMessageHasValues()
    {
        using var listener = new ActivityListener();
        listener.ShouldListenTo = _ => true;
        listener.Sample = SampleAllData;
        ActivitySource.AddActivityListener(listener);

        using var source = new ActivitySource("Test");
        using var activity = source.CreateActivity("bus", ActivityKind.Internal)?.Start();
        activity.ShouldNotBeNull();

        BusActivityTagger.AddScalarPropertyTags(new TestBusMessage { Number = "WO-1" }, activity);

        activity.GetTagItem("bus.message.Number").ShouldBe("WO-1");
    }

    private static ActivitySamplingResult SampleAllData(ref ActivityCreationOptions<ActivityContext> _) =>
        ActivitySamplingResult.AllData;

    private sealed class TestBusMessage
    {
        public string Number { get; init; } = string.Empty;
    }
}
