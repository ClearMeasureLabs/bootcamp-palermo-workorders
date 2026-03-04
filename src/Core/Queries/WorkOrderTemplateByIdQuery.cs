using ClearMeasure.Bootcamp.Core.Model;
using MediatR;

namespace ClearMeasure.Bootcamp.Core.Queries;

/// <summary>Retrieves a single work order template by its identifier.</summary>
public record WorkOrderTemplateByIdQuery(Guid Id) : IRequest<WorkOrderTemplate?>;
