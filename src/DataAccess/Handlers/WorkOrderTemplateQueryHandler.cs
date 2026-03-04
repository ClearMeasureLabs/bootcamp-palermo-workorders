using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

/// <summary>Handles queries for work order templates.</summary>
public class WorkOrderTemplateQueryHandler(DataContext context)
    : IRequestHandler<WorkOrderTemplatesQuery, WorkOrderTemplate[]>,
      IRequestHandler<WorkOrderTemplateByIdQuery, WorkOrderTemplate?>
{
    public async Task<WorkOrderTemplate[]> Handle(WorkOrderTemplatesQuery request,
        CancellationToken cancellationToken = default)
    {
        return await context.Set<WorkOrderTemplate>()
            .Where(t => t.IsActive)
            .OrderBy(t => t.Title)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<WorkOrderTemplate?> Handle(WorkOrderTemplateByIdQuery request,
        CancellationToken cancellationToken = default)
    {
        return await context.Set<WorkOrderTemplate>()
            .SingleOrDefaultAsync(t => t.Id == request.Id, cancellationToken);
    }
}
