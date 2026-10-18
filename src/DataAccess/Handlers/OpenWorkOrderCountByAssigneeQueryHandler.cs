using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

public class OpenWorkOrderCountByAssigneeQueryHandler(DataContext context) :
    IRequestHandler<OpenWorkOrderCountByAssigneeQuery, int>
{
    public Task<int> Handle(OpenWorkOrderCountByAssigneeQuery request,
        CancellationToken cancellationToken = default)
    {
        var assigned = WorkOrderStatus.Assigned;
        var inProgress = WorkOrderStatus.InProgress;
        return context.Set<WorkOrder>()
            .AsNoTracking()
            .CountAsync(wo =>
                wo.Assignee != null &&
                wo.Assignee.UserName == request.Username &&
                (wo.Status == assigned || wo.Status == inProgress),
                cancellationToken);
    }
}
