using ClearMeasure.Bootcamp.Core.Model.Messages;
using ClearMeasure.Bootcamp.UI.Server;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using NServiceBus;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class TracerBulletHealthCheckTests
{
    [Test]
    public async Task CheckHealthAsync_WhenReplyArrives_ShouldReturnHealthy()
    {
        var stubSession = new StubMessageSession();
        var healthCheck = new TracerBulletHealthCheck(
            stubSession,
            new StubLogger<TracerBulletHealthCheck>());
        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("tracerbullet", healthCheck, null, null)
        };

        stubSession.OnSend = cmd =>
        {
            if (cmd is TracerBulletCommand tracerCmd)
                TracerBulletSignal.Complete(tracerCmd.CorrelationId);
        };

        var result = await healthCheck.CheckHealthAsync(context);

        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Test]
    public async Task CheckHealthAsync_WhenSendThrows_ShouldReturnUnhealthy()
    {
        var stubSession = new StubMessageSession();
        var healthCheck = new TracerBulletHealthCheck(
            stubSession,
            new StubLogger<TracerBulletHealthCheck>());
        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("tracerbullet", healthCheck, null, null)
        };

        stubSession.OnSend = _ => throw new InvalidOperationException("NServiceBus unavailable");

        var result = await healthCheck.CheckHealthAsync(context);

        result.Status.ShouldBe(HealthStatus.Unhealthy);
    }

    [Test]
    public async Task CheckHealthAsync_WhenCancelled_ShouldReturnUnhealthy()
    {
        var stubSession = new StubMessageSession();
        var healthCheck = new TracerBulletHealthCheck(
            stubSession,
            new StubLogger<TracerBulletHealthCheck>());
        var context = new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("tracerbullet", healthCheck, null, null)
        };

        using var cts = new CancellationTokenSource();
        stubSession.OnSend = _ => cts.Cancel();

        var result = await healthCheck.CheckHealthAsync(context, cts.Token);

        result.Status.ShouldBe(HealthStatus.Unhealthy);
    }

    private class StubMessageSession : IMessageSession
    {
        public Action<object>? OnSend { get; set; }

        public Task Send(object message, SendOptions options)
        {
            OnSend?.Invoke(message);
            return Task.CompletedTask;
        }

        public Task Send<T>(Action<T> messageConstructor, SendOptions options) =>
            throw new NotImplementedException();

        public Task Publish(object message, PublishOptions options) =>
            throw new NotImplementedException();

        public Task Publish<T>(Action<T> messageConstructor, PublishOptions options) =>
            throw new NotImplementedException();

        public Task Subscribe(Type eventType, SubscribeOptions options) =>
            throw new NotImplementedException();

        public Task Unsubscribe(Type eventType, UnsubscribeOptions options) =>
            throw new NotImplementedException();
    }

    private class StubLogger<T> : ILogger<T>
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => false;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter) { }
    }
}
