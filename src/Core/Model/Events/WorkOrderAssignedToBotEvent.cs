namespace ClearMeasure.Bootcamp.Core.Model.Events;

public record WorkOrderAssignedToBotEvent(Guid CorrelationId, Guid WorkOrderId, Guid BotUserId);