using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

public class WorkOrderCountByStatusQueryHandler(DataContext context) :
    IRequestHandler<WorkOrderCountByStatusQuery, Dictionary<string, int>>
{
    public async Task<Dictionary<string, int>> Handle(WorkOrderCountByStatusQuery request,
        CancellationToken cancellationToken = default)
    {
        // GroupBy on a value-converter property cannot be translated to SQL.
        // Load statuses client-side and group in memory.
        var statuses = await context.Set<WorkOrder>()
            .AsNoTracking()
            .Select(wo => wo.Status)
            .ToListAsync(cancellationToken);

        var result = WorkOrderStatus.GetAllItems()
            .ToDictionary(s => s.Key, _ => 0);

        foreach (var status in statuses)
        {
            result[status.Key]++;
        }

        return result;
    }
}
