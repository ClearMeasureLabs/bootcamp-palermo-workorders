namespace ClearMeasure.Bootcamp.Core.Model.Events;

public record WorkOrderAssignedToBotEvent(Guid WorkOrderId, Guid BotUserId) : IStateTransitionEvent;