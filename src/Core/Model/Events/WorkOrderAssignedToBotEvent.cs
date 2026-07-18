namespace ClearMeasure.Bootcamp.Core.Model.Events;

public record WorkOrderAssignedToBotEvent(string WorkOrderNumber, Guid BotUserId) : IStateTransitionEvent;