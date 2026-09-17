using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

public class WorkOrderNotesQueryHandler(DataContext context)
    : IRequestHandler<WorkOrderNotesQuery, WorkOrderNote[]>
{
    public async Task<WorkOrderNote[]> Handle(WorkOrderNotesQuery request,
        CancellationToken cancellationToken = default)
    {
        return await context.Set<WorkOrderNote>()
            .Include(n => n.Author)
            .Where(n => n.WorkOrderId == request.WorkOrderId)
            .OrderByDescending(n => n.CreatedAt)
            .ToArrayAsync(cancellationToken);
    }
}
