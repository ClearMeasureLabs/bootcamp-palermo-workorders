using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

public class StateCommandHandler(DbContext dbContext, TimeProvider time, ILogger<StateCommandHandler> logger)
    : IRequestHandler<StateCommandBase, StateCommandResult>
{
    public async Task<StateCommandResult> Handle(StateCommandBase request,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Executing");
        var context = new StateCommandContext { CurrentDateTime = time.GetUtcNow().DateTime };
        request.Execute(context);

        var order = request.WorkOrder;
        if (order.Assignee == order.Creator)
        {
            order.Assignee = order.Creator; //EFCore reference checking
        }

        var isNewWorkOrder = order.Id == Guid.Empty;

        if (isNewWorkOrder)
        {
            dbContext.Attach(order);
            dbContext.Add(order);
        }
        else
        {
            dbContext.Attach(order);
            dbContext.Update(order);
        }

        await dbContext.SaveChangesAsync();

        // Add audit entries only for existing work orders (after SaveChanges so we have the Id)
        if (!isNewWorkOrder)
        {
            foreach (var auditEntry in context.AuditEntries)
            {
                // Ensure WorkOrderId is set
                if (auditEntry.WorkOrderId == Guid.Empty)
                {
                    auditEntry.WorkOrderId = order.Id;
                }
                dbContext.Add(auditEntry);
            }
            await dbContext.SaveChangesAsync();
        }

        var loweredTransitionVerb = request.TransitionVerbPastTense.ToLower();
        var workOrderNumber = order.Number;
        var fullName = request.CurrentUser.GetFullName();
        var debugMessage = string.Format("{0} has {1} work order {2}", fullName, loweredTransitionVerb,
            workOrderNumber);
        logger.LogDebug(debugMessage);
        logger.LogInformation("Executed");

        return new StateCommandResult(order, request.TransitionVerbPresentTense, debugMessage);
    }
}