namespace ClearMeasure.Bootcamp.Core.Model.Messages;

/// <summary>
/// Reply sent by the Worker back to the originating endpoint after handling a <see cref="TracerBulletCommand"/>.
/// Name ends in "Message" so <see cref="DataAccess.Messaging.MessagingConventions"/> recognizes it.
/// </summary>
public record TracerBulletReplyMessage(Guid CorrelationId);
