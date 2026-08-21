using Microsoft.Extensions.AI;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace ClearMeasure.Bootcamp.LlmGateway;

public class CanConnectToLlmServerHealthCheck(
    ChatClientFactory chatClientFactory,
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

        return await ProbeOrFailAsync(cancellationToken);
    }

    private HealthCheckResult LogUnavailable(ChatClientAvailabilityResult availability)
    {
        logger.LogWarning(availability.Message);
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
            logger.LogWarning("Chat client connection failed: {Message}", ex.Message);
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
        return LlmHealthEvaluator.FromChatResponse(response);
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
