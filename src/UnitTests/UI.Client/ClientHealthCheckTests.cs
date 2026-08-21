using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.UI.Client.HealthChecks;
using MediatR;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Client;

[TestFixture]
public class ClientHealthCheckTests
{
    [Test]
    public async Task RemotableBusHealthCheck_ShouldReturnHealthy_WhenBusHealthy()
    {
        var check = new RemotableBusHealthCheck(new StubHealthBus(HealthStatus.Healthy), NullLogger<RemotableBusHealthCheck>.Instance);

        var result = await check.CheckHealthAsync(new HealthCheckContext());

        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Description.ShouldNotBeNull().ShouldContain("healthy");
    }

    [Test]
    public async Task RemotableBusHealthCheck_ShouldReturnDegraded_WhenBusNotHealthy()
    {
        var check = new RemotableBusHealthCheck(new StubHealthBus(HealthStatus.Degraded), NullLogger<RemotableBusHealthCheck>.Instance);

        var result = await check.CheckHealthAsync(new HealthCheckContext());

        result.Status.ShouldBe(HealthStatus.Degraded);
        result.Description.ShouldNotBeNull().ShouldContain("Degraded");
    }

    [Test]
    public async Task RemotableBusHealthCheck_ShouldReturnUnhealthy_WhenBusThrows()
    {
        var check = new RemotableBusHealthCheck(new StubHealthBus(throwOnSend: true), NullLogger<RemotableBusHealthCheck>.Instance);

        var result = await check.CheckHealthAsync(new HealthCheckContext());

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Exception.ShouldNotBeNull();
    }

    [Test]
    public async Task ServerHealthCheck_ShouldReturnHealthy_WhenBusHealthy()
    {
        var check = new ServerHealthCheck(new StubHealthBus(HealthStatus.Healthy), NullLogger<ServerHealthCheck>.Instance);

        var result = await check.CheckHealthAsync(new HealthCheckContext());

        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Description.ShouldNotBeNull().ShouldContain("passed");
    }

    [Test]
    public async Task ServerHealthCheck_ShouldReturnUnhealthy_WhenBusThrows()
    {
        var check = new ServerHealthCheck(new StubHealthBus(throwOnSend: true), NullLogger<ServerHealthCheck>.Instance);

        var result = await check.CheckHealthAsync(new HealthCheckContext());

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Exception.ShouldNotBeNull();
    }

    [Test]
    public async Task ServerHealthCheck_ShouldReturnDegraded_WhenBusNotHealthy()
    {
        var check = new ServerHealthCheck(new StubHealthBus(HealthStatus.Degraded), NullLogger<ServerHealthCheck>.Instance);

        var result = await check.CheckHealthAsync(new HealthCheckContext());

        result.Status.ShouldBe(HealthStatus.Degraded);
        result.Description.ShouldNotBeNull().ShouldContain("Degraded");
    }

    [Test]
    public async Task HealthCheckTracer_ShouldReturnHealthy()
    {
        var check = new HealthCheckTracer(NullLogger<HealthCheckTracer>.Instance);

        var result = await check.CheckHealthAsync(new HealthCheckContext());

        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Description.ShouldBe("UI.Client is healthy");
    }

    private sealed class StubHealthBus(HealthStatus status = HealthStatus.Healthy, bool throwOnSend = false) : IBus
    {
        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
        {
            if (throwOnSend)
            {
                throw new InvalidOperationException("bus-down");
            }

            return Task.FromResult((TResponse)(object)status);
        }

        public Task<object?> Send(object request) => throw new NotSupportedException();

        public Task Publish(INotification notification) => throw new NotSupportedException();
    }
}
