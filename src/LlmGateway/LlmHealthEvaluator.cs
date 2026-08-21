using Microsoft.Extensions.AI;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ClearMeasure.Bootcamp.LlmGateway;

internal static class LlmHealthEvaluator
{
    /// <summary>
    /// When the chat client is not configured (AI_OpenAI_* environment variables absent), the
    /// chat/LLM feature is treated as intentionally disabled in that environment rather than as
    /// a failure. Reporting Healthy-with-info here (instead of Degraded) keeps overall
    /// application health accurate in environments — e.g. UAT/Prod — where the feature is off by
    /// policy. A chat client that IS configured but fails to connect or respond still reports
    /// Degraded/Unhealthy via <see cref="FromChatResponse"/> and <see cref="FromException"/>.
    /// </summary>
    public static HealthCheckResult FromAvailability(ChatClientAvailabilityResult availability) =>
        HealthCheckResult.Healthy(availability.IsAvailable
            ? availability.Message
            : $"{availability.Message} (chat feature not enabled in this environment)");

    public static HealthCheckResult FromChatResponse(ChatResponse response) =>
        response.Messages.Count > 0
            ? HealthCheckResult.Healthy("Chat client is connected")
            : HealthCheckResult.Degraded("Chat client returned empty response");

    public static HealthCheckResult FromException(Exception ex) =>
        HealthCheckResult.Unhealthy($"Chat client connection failed: {ex.Message}", ex);
}
