using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

public class WorkOrderCountByCreatorQueryHandler(DataContext context) :
    IRequestHandler<WorkOrderCountByCreatorQuery, int>
{
    public async Task<int> Handle(WorkOrderCountByCreatorQuery request,
        CancellationToken cancellationToken = default)
    {
        return await context.Set<WorkOrder>()
            .AsNoTracking()
            .CountAsync(wo => wo.Creator!.UserName == request.Username, cancellationToken);
    }
}
