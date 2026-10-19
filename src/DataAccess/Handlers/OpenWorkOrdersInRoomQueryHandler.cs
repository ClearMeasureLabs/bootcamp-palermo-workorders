using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

public class OpenWorkOrdersInRoomQueryHandler(DataContext context)
    : IRequestHandler<OpenWorkOrdersInRoomQuery, WorkOrder[]>
{
    public async Task<WorkOrder[]> Handle(OpenWorkOrdersInRoomQuery request,
        CancellationToken cancellationToken = default)
    {
        var candidates = await context.Set<WorkOrder>()
            .AsNoTracking()
            .Include(wo => wo.Creator)
            .Include(wo => wo.Assignee)
            .Where(wo => wo.RoomNumber == request.RoomNumber)
            .Where(wo => request.ExcludeNumber == null || wo.Number != request.ExcludeNumber)
            .ToListAsync(cancellationToken);

        return FilterOpenStatuses(candidates);
    }

    private static WorkOrder[] FilterOpenStatuses(List<WorkOrder> candidates)
    {
        return candidates
            .Where(IsOpenStatus)
            .ToArray();
    }

    private static bool IsOpenStatus(WorkOrder wo) =>
        wo.Status == WorkOrderStatus.Draft
        || wo.Status == WorkOrderStatus.Assigned
        || wo.Status == WorkOrderStatus.InProgress;
}
