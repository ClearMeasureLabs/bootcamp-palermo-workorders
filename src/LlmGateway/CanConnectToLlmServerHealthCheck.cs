using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace ClearMeasure.Bootcamp.LlmGateway;

public class CanConnectToLlmServerHealthCheck(
    IConfiguration configuration,
    ChatClientFactory chatClientFactory,
    ILogger<CanConnectToLlmServerHealthCheck> logger) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = new())
    {
        var apiKey = configuration.GetValue<string>("AI_OpenAI_ApiKey");
        var openAiUrl = configuration.GetValue<string>("AI_OpenAI_Url");
        var openAiModel = configuration.GetValue<string>("AI_OpenAI_Model");

        if (string.IsNullOrEmpty(openAiUrl))
        {
            var message = "AI_OpenAI_Url is not configured";
            logger.LogWarning(message);
            return HealthCheckResult.Unhealthy(message);
        }

        if (string.IsNullOrEmpty(apiKey))
        {
            var message = "AI_OpenAI_ApiKey is not configured";
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
                logger.LogInformation("Health check success via ChatClientFactory");
                return HealthCheckResult.Healthy($"Azure OpenAI endpoint reachable, model: {openAiModel}");
            }

            logger.LogWarning("Azure OpenAI returned empty response");
            return HealthCheckResult.Degraded($"$Azure OpenAI returned empty response from endpoint {openAiUrl}");
        }
        catch (Exception ex)
        {
            var message = $"Cannot connect to Azure OpenAI at {openAiUrl}: {ex.Message}";
            logger.LogWarning(message);
            return HealthCheckResult.Unhealthy(message);
        }
    }
}