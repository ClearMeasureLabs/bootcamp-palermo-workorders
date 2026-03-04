using ClearMeasure.Bootcamp.Core.Model;
using MediatR;

namespace ClearMeasure.Bootcamp.Core.Model.StateCommands;

/// <summary>Command to create a new work order pre-populated from an existing template.</summary>
public record CreateWorkOrderFromTemplateCommand(
    Guid TemplateId,
    Guid CreatorId) : IRequest<WorkOrder>, IRemotableRequest;
