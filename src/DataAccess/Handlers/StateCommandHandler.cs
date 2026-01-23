using ClearMeasure.Bootcamp.Core.Model;
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

        var beginStatus = request.WorkOrder.Status;
        var currentDateTime = time.GetUtcNow().DateTime;

        request.Execute(new StateCommandContext { CurrentDateTime = currentDateTime });

        var order = request.WorkOrder;
        var endStatus = order.Status;

        if (order.Assignee == order.Creator)
        {
            order.Assignee = order.Creator; //EFCore reference checking
        }

        if (order.Id == Guid.Empty)
        {
            dbContext.Attach(order);
            dbContext.Add(order);
        }
        else
        {
            dbContext.Attach(order);
            dbContext.Update(order);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        // Create audit entry for the action
        var auditEntry = CreateAuditEntry(order, request.CurrentUser, currentDateTime, 
            beginStatus, endStatus, request.TransitionVerbPresentTense);
        dbContext.Add(auditEntry);
        await dbContext.SaveChangesAsync(cancellationToken);

        var loweredTransitionVerb = request.TransitionVerbPastTense.ToLower();
        var workOrderNumber = order.Number;
        var fullName = request.CurrentUser.GetFullName();
        var debugMessage = string.Format("{0} has {1} work order {2}", fullName, loweredTransitionVerb,
            workOrderNumber);
        logger.LogDebug(debugMessage);
        logger.LogInformation("Executed");

        return new StateCommandResult(order, request.TransitionVerbPresentTense, debugMessage);
    }

    private static WorkOrderAuditEntry CreateAuditEntry(
        WorkOrder workOrder,
        Employee employee,
        DateTime date,
        WorkOrderStatus beginStatus,
        WorkOrderStatus endStatus,
        string actionVerb)
    {
        var isStatusChange = beginStatus != endStatus;
        var actionType = isStatusChange ? "StatusChange" : "Save";

        return new WorkOrderAuditEntry
        {
            Id = Guid.NewGuid(),
            WorkOrder = workOrder,
            Employee = employee,
            ArchivedEmployeeName = employee.GetFullName(),
            Date = date,
            BeginStatus = beginStatus,
            EndStatus = endStatus,
            ActionType = actionType,
            ActionDetails = actionVerb
        };
    }
}