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
        request.Execute(new StateCommandContext { CurrentDateTime = time.GetUtcNow().DateTime });

        var order = request.WorkOrder;
        if (order.Assignee == order.Creator)
        {
            order.Assignee = order.Creator; //EFCore reference checking
        }

        var isNewOrder = order.Id == Guid.Empty;
        if (isNewOrder)
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

        // Create audit entry for status change or save
        var auditEntry = CreateAuditEntry(order, request.CurrentUser, beginStatus, order.Status, request.TransitionVerbPresentTense);
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

    private AuditEntry CreateAuditEntry(WorkOrder workOrder, Employee employee, WorkOrderStatus beginStatus, WorkOrderStatus endStatus, string actionType)
    {
        var maxSequence = workOrder.AuditEntries.Any() 
            ? workOrder.AuditEntries.Max(a => a.Sequence) 
            : 0;

        return new AuditEntry
        {
            Id = Guid.NewGuid(),
            WorkOrderId = workOrder.Id,
            Sequence = maxSequence + 1,
            EmployeeId = employee.Id,
            ArchivedEmployeeName = employee.GetFullName(),
            Date = time.GetUtcNow().DateTime,
            BeginStatus = beginStatus,
            EndStatus = endStatus,
            ActionType = actionType
        };
    }
}