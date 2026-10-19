using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

public class OpenWorkOrderCountByAssigneeQueryHandler(DataContext context) :
    IRequestHandler<OpenWorkOrderCountByAssigneeQuery, Dictionary<string, int>>
{
    public async Task<Dictionary<string, int>> Handle(OpenWorkOrderCountByAssigneeQuery request,
        CancellationToken cancellationToken = default)
    {
        // GroupBy / Status equality on a value-converter property cannot be translated to SQL.
        // Load assignee + status client-side and aggregate in memory.
        var rows = await context.Set<WorkOrder>()
            .AsNoTracking()
            .Where(wo => wo.Assignee != null)
            .Select(wo => new { wo.Status, wo.Assignee!.UserName })
            .ToListAsync(cancellationToken);

        var result = new Dictionary<string, int>();
        foreach (var row in rows)
        {
            if (!IsOpen(row.Status))
            {
                continue;
            }

            result.TryGetValue(row.UserName, out var count);
            result[row.UserName] = count + 1;
        }

        return result;
    }

    private static bool IsOpen(WorkOrderStatus status) =>
        status == WorkOrderStatus.Assigned || status == WorkOrderStatus.InProgress;
}
