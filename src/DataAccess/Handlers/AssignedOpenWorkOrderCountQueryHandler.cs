using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

public class AssignedOpenWorkOrderCountQueryHandler(DataContext context)
    : IRequestHandler<AssignedOpenWorkOrderCountQuery, int>
{
    public async Task<int> Handle(AssignedOpenWorkOrderCountQuery request,
        CancellationToken cancellationToken = default)
    {
        return await context.Set<WorkOrder>()
            .CountAsync(
                wo => wo.Assignee != null
                      && wo.Assignee.UserName == request.UserName
                      && (wo.Status == WorkOrderStatus.Assigned || wo.Status == WorkOrderStatus.InProgress),
                cancellationToken);
    }
}
