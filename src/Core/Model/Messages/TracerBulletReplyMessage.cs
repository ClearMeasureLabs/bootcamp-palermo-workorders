namespace ClearMeasure.Bootcamp.Core.Model.Messages;

/// <summary>
/// Reply sent by the Worker back to the originating endpoint after handling a <see cref="TracerBulletCommand"/>.
/// Name ends in "Message" so DataAccess MessagingConventions recognizes it.
/// </summary>
public record TracerBulletReplyMessage(Guid CorrelationId);
