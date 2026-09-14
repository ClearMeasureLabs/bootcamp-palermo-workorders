using Microsoft.Extensions.AI;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ClearMeasure.Bootcamp.LlmGateway;

public class CanConnectToLlmServerHealthCheck(
    ChatClientFactory chatClientFactory,
    ILlmHealthCheckCache cache,
    IOptions<LlmHealthCheckOptions> options,
    ILogger<CanConnectToLlmServerHealthCheck> logger) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = new())
    {
        var availability = await chatClientFactory.IsChatClientAvailable();
        if (!availability.IsAvailable)
        {
            return LogUnavailable(availability);
        }

        if (cache.TryGet(options.Value.CacheDuration, out var cached, out var ageSeconds))
        {
            return LlmHealthEvaluator.Annotate(cached, true, ageSeconds);
        }

        return await ProbeOrFailAsync(cancellationToken);
    }

    private HealthCheckResult LogUnavailable(ChatClientAvailabilityResult availability)
    {
        logger.LogDebug(availability.Message);
        return LlmHealthEvaluator.FromAvailability(availability);
    }

    private async Task<HealthCheckResult> ProbeOrFailAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await ProbeChatClientAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Chat client connection failed");
            return LlmHealthEvaluator.FromException(ex);
        }
    }

    private async Task<HealthCheckResult> ProbeChatClientAsync(CancellationToken cancellationToken)
    {
        var chatClient = await chatClientFactory.GetChatClient();
        var response = await chatClient.GetResponseAsync(
            [new ChatMessage(ChatRole.User, "Reply with OK")],
            cancellationToken: cancellationToken);

        LogProbeOutcome(response);
        var result = LlmHealthEvaluator.FromChatResponse(response);

        if (result.Status != HealthStatus.Unhealthy)
        {
            cache.Set(result, DateTimeOffset.UtcNow);
        }

        return LlmHealthEvaluator.Annotate(result, false, 0);
    }

    private void LogProbeOutcome(ChatResponse response)
    {
        if (response.Messages.Count > 0)
        {
            logger.LogDebug("Health check success via ChatClientFactory");
        }
        else
        {
            logger.LogWarning("Chat client returned empty response");
        }
    }
}
