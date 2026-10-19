using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

public class WorkOrdersDueSoonQueryHandler(DataContext context, TimeProvider timeProvider) :
    IRequestHandler<WorkOrdersDueSoonQuery, WorkOrder[]>
{
    public async Task<WorkOrder[]> Handle(WorkOrdersDueSoonQuery request,
        CancellationToken cancellationToken = default)
    {
        var today = ChurchTimeZone.Today(timeProvider);
        var endDate = today.AddDays(request.Days);

        var candidates = await context.Set<WorkOrder>()
            .AsNoTracking()
            .Include(wo => wo.Creator)
            .Include(wo => wo.Assignee)
            .Where(wo => wo.DueDate != null && wo.DueDate >= today && wo.DueDate <= endDate)
            .OrderBy(wo => wo.DueDate)
            .ToListAsync(cancellationToken);

        return candidates.Where(IsOpenStatus).ToArray();
    }

    private static bool IsOpenStatus(WorkOrder wo) =>
        wo.Status == WorkOrderStatus.Draft
        || wo.Status == WorkOrderStatus.Assigned
        || wo.Status == WorkOrderStatus.InProgress;
}
