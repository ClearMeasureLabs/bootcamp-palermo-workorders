using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Api;
using ClearMeasure.Bootcamp.UI.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class MetricsSummaryBuilderTests
{
    private sealed class FixedUtcTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private sealed class StubRequestMetricsStore(long totalRequests) : IRequestMetricsStore
    {
        public long TotalRequests { get; } = totalRequests;

        public void Increment()
        {
        }
    }

    [Test]
    public void Should_Return_UptimeFromTimeProvider_When_Build()
    {
        var clock = new FixedUtcTimeProvider(new DateTimeOffset(2026, 4, 10, 15, 0, 0, TimeSpan.Zero));
        var store = new StubRequestMetricsStore(0);

        var response = MetricsSummaryBuilder.Build(clock, store);

        response.Uptime.ShouldBe(SimpleHealthResponseBuilder.Build(clock).Uptime);
    }

    [Test]
    public void Should_Return_TotalRequestsFromStore_When_Build()
    {
        var clock = new FixedUtcTimeProvider(new DateTimeOffset(2026, 4, 10, 15, 0, 0, TimeSpan.Zero));
        var store = new StubRequestMetricsStore(42);

        var response = MetricsSummaryBuilder.Build(clock, store);

        response.TotalRequests.ShouldBe(42);
    }

    [Test]
    public void Should_Return_GcMemoryAndWorkingSetMb_When_Build()
    {
        var clock = new FixedUtcTimeProvider(new DateTimeOffset(2026, 4, 10, 15, 0, 0, TimeSpan.Zero));
        var store = new StubRequestMetricsStore(0);

        var response = MetricsSummaryBuilder.Build(clock, store);

        response.GcMemoryMb.ShouldBeGreaterThanOrEqualTo(0);
        response.WorkingSetMb.ShouldBeGreaterThanOrEqualTo(0);
    }

    [Test]
    public void Should_Return_GcCollectionCounts_When_Build()
    {
        var clock = new FixedUtcTimeProvider(new DateTimeOffset(2026, 4, 10, 15, 0, 0, TimeSpan.Zero));
        var store = new StubRequestMetricsStore(0);

        var response = MetricsSummaryBuilder.Build(clock, store);

        response.GcCollections.Gen0.ShouldBe(GC.CollectionCount(0));
        response.GcCollections.Gen1.ShouldBe(GC.CollectionCount(1));
        response.GcCollections.Gen2.ShouldBe(GC.CollectionCount(2));
        response.GcCollections.Gen0.ShouldBeGreaterThanOrEqualTo(0);
        response.GcCollections.Gen1.ShouldBeGreaterThanOrEqualTo(0);
        response.GcCollections.Gen2.ShouldBeGreaterThanOrEqualTo(0);
    }
}
