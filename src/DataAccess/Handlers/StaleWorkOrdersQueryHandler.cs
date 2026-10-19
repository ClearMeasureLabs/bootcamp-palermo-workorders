using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

public class StaleWorkOrdersQueryHandler(DataContext context) :
    IRequestHandler<StaleWorkOrdersQuery, WorkOrder[]>
{
    public async Task<WorkOrder[]> Handle(StaleWorkOrdersQuery request,
        CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow.AddDays(-request.Days);

        return await context.Set<WorkOrder>()
            .AsNoTracking()
            .Include(wo => wo.Creator)
            .Include(wo => wo.Assignee)
            .Where(wo =>
                (wo.Status == WorkOrderStatus.Assigned || wo.Status == WorkOrderStatus.InProgress)
                && wo.AssignedDate != null
                && wo.AssignedDate < cutoff)
            .OrderBy(wo => wo.AssignedDate)
            .ToArrayAsync(cancellationToken);
    }
}
