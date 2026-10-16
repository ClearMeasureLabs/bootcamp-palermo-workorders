using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

public class OpenWorkOrderCountQueryHandler(DataContext context) :
    IRequestHandler<OpenWorkOrderCountQuery, int>
{
    public async Task<int> Handle(OpenWorkOrderCountQuery request,
        CancellationToken cancellationToken = default)
    {
        // Local status variable required for EF Core value-converter translation
        // (same pattern as WorkOrderQueryFilters).
        var complete = WorkOrderStatus.Complete;
        return await context.Set<WorkOrder>()
            .AsNoTracking()
            .CountAsync(wo => wo.Status != complete, cancellationToken);
    }
}
