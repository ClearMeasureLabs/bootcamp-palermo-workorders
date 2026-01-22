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
        var previousStatus = request.WorkOrder.Status;
        request.Execute(new StateCommandContext { CurrentDateTime = time.GetUtcNow().DateTime });

        var order = request.WorkOrder;
        if (order.Assignee == order.Creator)
        {
            order.Assignee = order.Creator; //EFCore reference checking
        }

        // Create audit entry for status change or save
        var currentDateTime = time.GetUtcNow().DateTime;
        var nextSequence = order.AuditEntries.Any() 
            ? order.AuditEntries.Max(a => a.Sequence) + 1 
            : 1;
        
        var auditEntry = new AuditEntry
        {
            Id = Guid.NewGuid(),
            WorkOrderId = order.Id,
            Sequence = nextSequence,
            EmployeeId = request.CurrentUser.Id,
            Employee = request.CurrentUser,
            ArchivedEmployeeName = request.CurrentUser.GetFullName(),
            Date = currentDateTime,
            BeginStatus = previousStatus,
            EndStatus = order.Status
        };
        
        order.AuditEntries.Add(auditEntry);

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

        await dbContext.SaveChangesAsync();

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