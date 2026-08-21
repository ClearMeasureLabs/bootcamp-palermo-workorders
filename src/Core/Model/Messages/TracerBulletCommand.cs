namespace ClearMeasure.Bootcamp.Core.Model.Messages;

/// <summary>
/// Tracer bullet command sent from the test endpoint to the Worker endpoint.
/// The Worker handler replies with <see cref="TracerBulletReplyMessage"/>.
/// Name ends in "Command" so DataAccess MessagingConventions recognizes it.
/// </summary>
public record TracerBulletCommand(Guid CorrelationId);
