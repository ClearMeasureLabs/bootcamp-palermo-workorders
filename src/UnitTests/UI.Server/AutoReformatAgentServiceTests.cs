using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.Events;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.LlmGateway;
using ClearMeasure.Bootcamp.UI.Server;
using ClearMeasure.Bootcamp.UI.Server.Notifications;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class AutoReformatAgentServiceTests
{
    [Test]
    public void IsDisabled_ReturnsTrue_WhenConfigurationTrue()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["DISABLE_AUTO_REFORMAT_AGENT"] = "true" })
            .Build();

        AutoReformatAgentService.IsDisabled(config).ShouldBeTrue();
    }

    [Test]
    public void IsDisabled_ReturnsFalse_WhenConfigurationMissing()
    {
        var config = new ConfigurationBuilder().Build();
        AutoReformatAgentService.IsDisabled(config).ShouldBeFalse();
    }

    [Test]
    public async Task LoadDraftWorkOrdersAsync_ReturnsDraftOrders()
    {
        var workOrder = new WorkOrder { Number = "WO-1", Status = WorkOrderStatus.Draft };
        var bus = new StubDraftWorkOrderBus([workOrder]);

        var results = await AutoReformatAgentService.LoadDraftWorkOrdersAsync(bus);

        results.Length.ShouldBe(1);
        results[0].Number.ShouldBe("WO-1");
    }

    [Test]
    public async Task ReformatWorkOrdersAsync_DoesNotThrow_WhenNoDraftOrders()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IBus>(new StubDraftWorkOrderBus([]));
        services.AddSingleton(new WorkOrderReformatAgent(new ChatClientFactory(new StubUnavailableBus()), NullLogger<WorkOrderReformatAgent>.Instance));
        var provider = services.BuildServiceProvider();
        var service = new AutoReformatAgentService(
            provider,
            NullLogger<AutoReformatAgentService>.Instance,
            new ConfigurationBuilder().Build(),
            TimeProvider.System);

        await service.ReformatWorkOrdersAsync();
    }

    [Test]
    public async Task ExecuteAsync_ReturnsImmediately_WhenDisabled()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IBus>(new StubDraftWorkOrderBus([]));
        services.AddSingleton(new WorkOrderReformatAgent(new ChatClientFactory(new StubUnavailableBus()), NullLogger<WorkOrderReformatAgent>.Instance));
        var provider = services.BuildServiceProvider();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["DISABLE_AUTO_REFORMAT_AGENT"] = "true" })
            .Build();
        var service = new AutoReformatAgentService(
            provider,
            NullLogger<AutoReformatAgentService>.Instance,
            config,
            TimeProvider.System);

        await service.StartAsync(CancellationToken.None);
        await service.StopAsync(CancellationToken.None);
    }

    [Test]
    public async Task ExecuteAsync_ExitsLoop_WhenCancelledDuringDelay()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IBus>(new StubDraftWorkOrderBus([]));
        services.AddSingleton(new WorkOrderReformatAgent(new ChatClientFactory(new StubUnavailableBus()), NullLogger<WorkOrderReformatAgent>.Instance));
        var provider = services.BuildServiceProvider();
        var service = new AutoReformatAgentService(
            provider,
            NullLogger<AutoReformatAgentService>.Instance,
            new ConfigurationBuilder().Build(),
            TimeProvider.System);

        using var cts = new CancellationTokenSource();
        var start = service.StartAsync(cts.Token);
        await Task.Delay(50);
        cts.Cancel();
        await service.StopAsync(CancellationToken.None);
        await start;
    }

    [Test]
    public async Task ExecuteAsync_Continues_WhenReformatThrowsThenCancelled()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IBus>(new StubThrowingBus());
        services.AddSingleton(new WorkOrderReformatAgent(new ChatClientFactory(new StubUnavailableBus()), NullLogger<WorkOrderReformatAgent>.Instance));
        var provider = services.BuildServiceProvider();
        var service = new AutoReformatAgentService(
            provider,
            NullLogger<AutoReformatAgentService>.Instance,
            new ConfigurationBuilder().Build(),
            TimeProvider.System);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
        try
        {
            await service.StartAsync(cts.Token);
            await Task.Delay(300);
        }
        catch (OperationCanceledException)
        {
        }

        await service.StopAsync(CancellationToken.None);
    }

    private sealed class StubThrowingBus : IBus
    {
        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request) =>
            throw new InvalidOperationException("reformat-query-failed");

        public Task<object?> Send(object request) => throw new NotSupportedException();

        public Task Publish(INotification notification) => throw new NotSupportedException();
    }

    private sealed class StubDraftWorkOrderBus(WorkOrder[] workOrders) : IBus
    {
        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
        {
            if (request is WorkOrderSpecificationQuery)
            {
                return Task.FromResult((TResponse)(object)workOrders);
            }

            throw new NotSupportedException();
        }

        public Task<object?> Send(object request) => throw new NotSupportedException();

        public Task Publish(INotification notification) => throw new NotSupportedException();
    }

    private sealed class StubUnavailableBus : IBus
    {
        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request) => throw new NotSupportedException();

        public Task<object?> Send(object request) => throw new NotSupportedException();

        public Task Publish(INotification notification) => throw new NotSupportedException();
    }
}
