using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

public class WorkOrderCountByAssigneeQueryHandler(DataContext context) :
    IRequestHandler<WorkOrderCountByAssigneeQuery, int>
{
    public Task<int> Handle(WorkOrderCountByAssigneeQuery request,
        CancellationToken cancellationToken = default)
    {
        return context.Set<WorkOrder>()
            .AsNoTracking()
            .CountAsync(wo => wo.Assignee != null && wo.Assignee.UserName == request.Username,
                cancellationToken);
    }
}
