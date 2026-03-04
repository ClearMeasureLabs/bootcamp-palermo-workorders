using ClearMeasure.Bootcamp.Core;
using MediatR;

namespace ClearMeasure.Bootcamp.Core.Model.StateCommands;

/// <summary>Toggles the IsCompleted flag on a subtask.</summary>
public record ToggleSubtaskCommand(Guid SubtaskId) : IRequest<WorkOrderSubtask>, IRemotableRequest;
