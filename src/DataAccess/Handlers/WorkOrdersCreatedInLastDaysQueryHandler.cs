using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

public class WorkOrdersCreatedInLastDaysQueryHandler(DataContext context) :
    IRequestHandler<WorkOrdersCreatedInLastDaysQuery, WorkOrder[]>
{
    public async Task<WorkOrder[]> Handle(WorkOrdersCreatedInLastDaysQuery request,
        CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow.AddDays(-request.Days);

        return await context.Set<WorkOrder>()
            .AsNoTracking()
            .Include(wo => wo.Creator)
            .Include(wo => wo.Assignee)
            .Where(wo => wo.CreatedDate >= cutoff)
            .OrderByDescending(wo => wo.CreatedDate)
            .ToArrayAsync(cancellationToken);
    }
}
