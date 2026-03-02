using ClearMeasure.Bootcamp.Core.Model.Messages;

namespace ClearMeasure.Bootcamp.UI.Server;

/// <summary>
/// Health check that sends a <see cref="TracerBulletCommand"/> through NServiceBus to the Worker
/// endpoint and waits for a <see cref="TracerBulletReplyMessage"/> reply, verifying the full
/// Send/Reply pipeline is operational.
/// </summary>
public class TracerBulletHealthCheck(
    IMessageSession messageSession,
    ILogger<TracerBulletHealthCheck> logger) : IHealthCheck
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(60);

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var correlationId = Guid.NewGuid();

        try
        {
            var replyTask = TracerBulletSignal.WaitForReply(correlationId, Timeout);

            await messageSession.Send(new TracerBulletCommand(correlationId));
            logger.LogInformation(
                "TracerBullet health check: command sent with CorrelationId={CorrelationId}",
                correlationId);

            await replyTask;
            logger.LogInformation(
                "TracerBullet health check: reply received for CorrelationId={CorrelationId}",
                correlationId);

            return HealthCheckResult.Healthy("Worker round-trip succeeded.");
        }
        catch (TimeoutException)
        {
            logger.LogWarning(
                "TracerBullet health check: timed out waiting for reply. CorrelationId={CorrelationId}",
                correlationId);
            return HealthCheckResult.Unhealthy("Worker round-trip timed out after 60 seconds.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "TracerBullet health check: failed. CorrelationId={CorrelationId}",
                correlationId);
            return HealthCheckResult.Unhealthy("Worker round-trip failed.", ex);
        }
    }
}
