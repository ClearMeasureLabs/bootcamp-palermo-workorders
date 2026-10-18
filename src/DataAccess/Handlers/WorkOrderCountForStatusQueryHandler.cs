using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

public class WorkOrderCountForStatusQueryHandler(DataContext context) :
    IRequestHandler<WorkOrderCountForStatusQuery, int>
{
    public async Task<int> Handle(WorkOrderCountForStatusQuery request,
        CancellationToken cancellationToken = default)
    {
        var status = WorkOrderStatus.FromKey(request.StatusKey);
        return await context.Set<WorkOrder>()
            .AsNoTracking()
            .CountAsync(wo => wo.Status == status, cancellationToken);
    }
}
