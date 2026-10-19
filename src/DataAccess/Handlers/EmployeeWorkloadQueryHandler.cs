using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

/// <summary>
/// Counts Assigned and In Progress work orders for a single employee by username.
/// </summary>
public class EmployeeWorkloadQueryHandler(DataContext context)
    : IRequestHandler<EmployeeWorkloadQuery, EmployeeWorkloadResult?>
{
    public async Task<EmployeeWorkloadResult?> Handle(EmployeeWorkloadQuery request,
        CancellationToken cancellationToken = default)
    {
        var employeeExists = await context.Set<Employee>()
            .AsNoTracking()
            .AnyAsync(emp => emp.UserName == request.Username, cancellationToken);

        if (!employeeExists)
        {
            return null;
        }

        // Status uses a value converter; load client-side then count in memory.
        var statuses = await context.Set<WorkOrder>()
            .AsNoTracking()
            .Where(wo => wo.Assignee != null && wo.Assignee.UserName == request.Username)
            .Where(wo => wo.Status == WorkOrderStatus.Assigned || wo.Status == WorkOrderStatus.InProgress)
            .Select(wo => wo.Status)
            .ToListAsync(cancellationToken);

        var assignedCount = statuses.Count(s => s == WorkOrderStatus.Assigned);
        var inProgressCount = statuses.Count(s => s == WorkOrderStatus.InProgress);

        return new EmployeeWorkloadResult(request.Username, assignedCount, inProgressCount);
    }
}
