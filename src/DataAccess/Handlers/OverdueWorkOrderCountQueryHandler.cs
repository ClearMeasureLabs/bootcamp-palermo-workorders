using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

public class OverdueWorkOrderCountQueryHandler(DataContext context) :
    IRequestHandler<OverdueWorkOrderCountQuery, int>
{
    public async Task<int> Handle(OverdueWorkOrderCountQuery request,
        CancellationToken cancellationToken = default)
    {
        var asOfDate = request.AsOfDate;
        var draft = WorkOrderStatus.Draft;
        var assigned = WorkOrderStatus.Assigned;
        var inProgress = WorkOrderStatus.InProgress;

        return await context.Set<WorkOrder>()
            .AsNoTracking()
            .CountAsync(wo =>
                    wo.DueDate < asOfDate &&
                    (wo.Status == draft || wo.Status == assigned || wo.Status == inProgress),
                cancellationToken);
    }
}
