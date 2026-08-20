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
            logger.LogWarning(availability.Message);
            return LlmHealthEvaluator.FromAvailability(availability);
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
            }
            else
            {
                logger.LogWarning("Chat client returned empty response");
            }

            return LlmHealthEvaluator.FromChatResponse(response);
        }
        catch (Exception ex)
        {
            var message = $"Chat client connection failed: {ex.Message}";
            logger.LogWarning(message);
            return LlmHealthEvaluator.FromException(ex);
        }
    }
}
