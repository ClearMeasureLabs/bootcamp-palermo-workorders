using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

public class ReassignWorkOrderCommandHandler(DbContext dbContext, ILogger<ReassignWorkOrderCommandHandler> logger)
    : IRequestHandler<ReassignWorkOrderCommand, StateCommandResult>
{
    public async Task<StateCommandResult> Handle(ReassignWorkOrderCommand request,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Executing reassignment");
        request.Execute();

        var order = request.WorkOrder;
        if (order.Creator?.Id == request.NewAssignee.Id)
        {
            order.Assignee = order.Creator;
        }

        dbContext.Attach(order);
        dbContext.Update(order);
        await dbContext.SaveChangesAsync(cancellationToken);

        var message = $"{request.RequestedBy.GetFullName()} has reassigned work order {order.Number}";
        logger.LogDebug("{Message}", message);
        logger.LogInformation("Executed");
        return new StateCommandResult(order, ReassignWorkOrderCommand.Name, message);
    }
}
