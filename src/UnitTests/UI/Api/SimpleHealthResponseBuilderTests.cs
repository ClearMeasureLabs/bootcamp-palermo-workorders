using ClearMeasure.Bootcamp.UI.Api;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class SimpleHealthResponseBuilderTests
{
    private sealed class FixedUtcTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    [Test]
    public void Build_Should_SetStatusHealthy_When_Default()
    {
        var clock = new FixedUtcTimeProvider(new DateTimeOffset(2026, 3, 30, 12, 0, 0, TimeSpan.Zero));
        var start = new DateTimeOffset(2026, 3, 30, 11, 0, 0, TimeSpan.Zero);

        var response = SimpleHealthResponseBuilder.Build(clock, start);

        response.Status.ShouldBe(SimpleHealthStatus.Healthy);
    }

    [Test]
    public void Build_Should_SetCurrentTimeUtc_FromTimeProvider_When_Called()
    {
        var fixedNow = new DateTimeOffset(2026, 3, 30, 15, 30, 45, TimeSpan.Zero);
        var clock = new FixedUtcTimeProvider(fixedNow);
        var start = new DateTimeOffset(2026, 3, 30, 10, 0, 0, TimeSpan.Zero);

        var response = SimpleHealthResponseBuilder.Build(clock, start);

        response.CurrentTimeUtc.ShouldBe(fixedNow.UtcDateTime);
        response.CurrentTimeUtc.Kind.ShouldBe(DateTimeKind.Utc);
    }

    [Test]
    public void Build_Should_ComputeUptime_AsNowMinusStart_When_Called()
    {
        var clock = new FixedUtcTimeProvider(new DateTimeOffset(2026, 3, 30, 14, 0, 0, TimeSpan.Zero));
        var start = new DateTimeOffset(2026, 3, 30, 12, 30, 0, TimeSpan.Zero);

        var response = SimpleHealthResponseBuilder.Build(clock, start);

        response.Uptime.ShouldBe(TimeSpan.FromHours(1.5));
    }
}
