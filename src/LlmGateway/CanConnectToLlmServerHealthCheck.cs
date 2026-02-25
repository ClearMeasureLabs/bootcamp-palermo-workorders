using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace ClearMeasure.Bootcamp.LlmGateway;

public class CanConnectToLlmServerHealthCheck(
    IConfiguration configuration,
    ILogger<CanConnectToLlmServerHealthCheck> logger) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = new())
    {
        var apiKey = configuration.GetValue<string>("AI_OpenAI_ApiKey");
        var openAiUrl = configuration.GetValue<string>("AI_OpenAI_Url");
        var openAiModel = configuration.GetValue<string>("AI_OpenAI_Model");

        var missing = new List<string>();
        if (string.IsNullOrEmpty(apiKey)) missing.Add("AI_OpenAI_ApiKey");
        if (string.IsNullOrEmpty(openAiUrl)) missing.Add("AI_OpenAI_Url");
        if (string.IsNullOrEmpty(openAiModel)) missing.Add("AI_OpenAI_Model");

        if (missing.Count > 0)
        {
            var message = $"Azure OpenAI environment variables not set: {string.Join(", ", missing)}";
            logger.LogWarning(message);
            return HealthCheckResult.Unhealthy(message);
        }

        try
        {
            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            var baseUrl = openAiUrl!.TrimEnd('/');
            var response = await httpClient.GetAsync(
                $"{baseUrl}/openai/models?api-version=2024-06-01",
                cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized ||
                response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                logger.LogWarning("Azure OpenAI endpoint reachable but API key is invalid");
                return HealthCheckResult.Unhealthy(
                    "Azure OpenAI endpoint is reachable but the API key is invalid");
            }

            logger.LogInformation("Health check success");
            return HealthCheckResult.Healthy($"Azure OpenAI endpoint reachable, model: {openAiModel}");
        }
        catch (Exception ex)
        {
            var message = $"Cannot connect to Azure OpenAI at {openAiUrl}: {ex.Message}";
            logger.LogWarning(message);
            return HealthCheckResult.Unhealthy(message);
        }
    }
}