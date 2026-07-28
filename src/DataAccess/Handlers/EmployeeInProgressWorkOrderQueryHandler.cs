using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

public class EmployeeInProgressWorkOrderQueryHandler(DataContext context) :
    IRequestHandler<EmployeeInProgressWorkOrderQuery, WorkOrder?>
{
    public async Task<WorkOrder?> Handle(EmployeeInProgressWorkOrderQuery request,
        CancellationToken cancellationToken = default)
    {
        return await context.Set<WorkOrder>()
            .FirstOrDefaultAsync(wo => wo.Assignee == request.Employee && wo.Status == WorkOrderStatus.InProgress, cancellationToken);
    }
}
