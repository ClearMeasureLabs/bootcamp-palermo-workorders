using ClearMeasure.Bootcamp.Core.Model;
using MediatR;

namespace ClearMeasure.Bootcamp.Core.Queries;

/// <summary>Retrieves all active work order templates.</summary>
public record WorkOrderTemplatesQuery : IRequest<WorkOrderTemplate[]>;
