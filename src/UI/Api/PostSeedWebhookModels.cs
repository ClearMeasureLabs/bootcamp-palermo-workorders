namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Payload accepted by the inbound post-seed webhook from automation after database seed completes.
/// </summary>
public sealed record PostSeedWebhookRequest(string Event, string? CorrelationId, object? Metadata);

/// <summary>
/// Minimal acknowledgment returned when the webhook is processed successfully.
/// </summary>
public sealed record PostSeedWebhookResponse(bool Received, bool SeedDataDetected);
