// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable NotAccessedPositionalProperty.Local
namespace ClearMeasure.Bootcamp.Core.Model.Events;

public record WorkOrderAssignedToBotEvent(string WorkOrderNumber, Guid BotUserId) : IStateTransitionEvent;