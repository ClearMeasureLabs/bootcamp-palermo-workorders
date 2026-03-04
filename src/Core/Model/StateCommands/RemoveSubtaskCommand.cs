using ClearMeasure.Bootcamp.Core;
using MediatR;

namespace ClearMeasure.Bootcamp.Core.Model.StateCommands;

/// <summary>Removes a subtask from a work order.</summary>
public record RemoveSubtaskCommand(Guid SubtaskId) : IRequest<bool>, IRemotableRequest;
