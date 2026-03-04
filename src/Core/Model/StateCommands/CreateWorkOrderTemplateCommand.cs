using ClearMeasure.Bootcamp.Core.Model;
using MediatR;

namespace ClearMeasure.Bootcamp.Core.Model.StateCommands;

/// <summary>Command to create a new work order template.</summary>
public record CreateWorkOrderTemplateCommand(
    string Title,
    string? Description,
    string? RoomNumber,
    Guid CreatedById) : IRequest<WorkOrderTemplate>;
