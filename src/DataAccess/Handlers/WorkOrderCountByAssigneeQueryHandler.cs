using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

public class WorkOrderCountByAssigneeQueryHandler(DataContext context) :
    IRequestHandler<WorkOrderCountByAssigneeQuery, Dictionary<string, int>>
{
    public async Task<Dictionary<string, int>> Handle(WorkOrderCountByAssigneeQuery request,
        CancellationToken cancellationToken = default)
    {
        var rows = await context.Set<WorkOrder>()
            .AsNoTracking()
            .Where(wo => wo.Assignee != null)
            .Select(wo => new { UserName = wo.Assignee!.UserName, wo.Status })
            .ToListAsync(cancellationToken);

        return CountOpenByAssignee(rows.Select(r => (r.UserName, r.Status)));
    }

    private static Dictionary<string, int> CountOpenByAssignee(
        IEnumerable<(string UserName, WorkOrderStatus Status)> rows)
    {
        var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var (userName, status) in rows)
        {
            if (status.Key == WorkOrderStatus.Complete.Key
                || status.Key == WorkOrderStatus.Cancelled.Key)
            {
                continue;
            }

            if (result.TryGetValue(userName, out var count))
            {
                result[userName] = count + 1;
            }
            else
            {
                result[userName] = 1;
            }
        }

        return result;
    }
}
