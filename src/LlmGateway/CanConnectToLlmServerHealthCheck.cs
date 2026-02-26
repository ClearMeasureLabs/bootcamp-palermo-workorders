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
        if (!await chatClientFactory.IsChatClientAvailable())
        {
            const string message = "Chat client is not configured";
            logger.LogWarning(message);
            return HealthCheckResult.Degraded(message);
        }

        try
        {
            var chatClient = await chatClientFactory.GetChatClient();
            var response = await chatClient.GetResponseAsync(
                [new ChatMessage(ChatRole.User, "Reply with OK")],
                cancellationToken: cancellationToken);

            if (response.Messages.Count > 0)
            {
                logger.LogDebug("Health check success via ChatClientFactory");
                return HealthCheckResult.Healthy("Chat client is connected");
            }

            logger.LogWarning("Chat client returned empty response");
            return HealthCheckResult.Degraded("Chat client returned empty response");
        }
        catch (Exception ex)
        {
            var message = $"Chat client connection failed: {ex.Message}";
            logger.LogWarning(message);
            return HealthCheckResult.Unhealthy(message);
        }
    }
}
