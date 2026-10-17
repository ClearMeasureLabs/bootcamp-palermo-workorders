namespace ClearMeasure.Bootcamp.Core.Model.Events;

public record WorkOrderCompletedEvent(string Number, string AssigneeName) : IStateTransitionEvent;
