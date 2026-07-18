namespace ClearMeasure.Bootcamp.Core.Model.Events;

public record WorkRequestAssignedToBotEvent(string WorkRequestNumber, Guid BotUserId) : IStateTransitionEvent;