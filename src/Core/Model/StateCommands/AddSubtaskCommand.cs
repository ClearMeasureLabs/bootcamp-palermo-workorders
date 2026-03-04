using ClearMeasure.Bootcamp.Core;
using MediatR;

namespace ClearMeasure.Bootcamp.Core.Model.StateCommands;

/// <summary>Adds a subtask to a work order.</summary>
public record AddSubtaskCommand(Guid WorkOrderId, string Title, int SortOrder) : IRequest<WorkOrderSubtask>, IRemotableRequest;
