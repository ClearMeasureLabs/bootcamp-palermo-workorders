using Microsoft.Extensions.AI;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ClearMeasure.Bootcamp.LlmGateway;

internal static class LlmHealthEvaluator
{
    public static HealthCheckResult FromAvailability(ChatClientAvailabilityResult availability) =>
        availability.IsAvailable
            ? HealthCheckResult.Healthy(availability.Message)
            : HealthCheckResult.Degraded(availability.Message);

    public static HealthCheckResult FromChatResponse(ChatResponse response) =>
        response.Messages.Count > 0
            ? HealthCheckResult.Healthy("Chat client is connected")
            : HealthCheckResult.Degraded("Chat client returned empty response");

    public static HealthCheckResult FromException(Exception ex) =>
        HealthCheckResult.Unhealthy($"Chat client connection failed: {ex.Message}", ex);
}
