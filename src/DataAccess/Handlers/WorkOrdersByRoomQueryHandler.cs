using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

public class WorkOrdersByRoomQueryHandler(DataContext context)
    : IRequestHandler<WorkOrdersByRoomQuery, WorkOrder[]>
{
    public async Task<WorkOrder[]> Handle(WorkOrdersByRoomQuery request,
        CancellationToken cancellationToken = default)
    {
        return await context.Set<WorkOrder>()
            .AsNoTracking()
            .Where(wo => wo.RoomNumber == request.RoomNumber)
            .ToArrayAsync(cancellationToken);
    }
}
