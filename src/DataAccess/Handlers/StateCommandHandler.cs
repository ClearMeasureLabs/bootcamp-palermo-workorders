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
        var currentDateTime = time.GetUtcNow().DateTime;
        var beginStatus = request.WorkOrder.Status;
        
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

        // Create audit entry for every status change and every save or edit
        var auditEntry = new AuditEntry
        {
            Id = Guid.NewGuid(),
            WorkOrderId = order.Id,
            EmployeeId = request.CurrentUser.Id,
            EmployeeName = request.CurrentUser.GetFullName(),
            Date = currentDateTime,
            BeginStatus = beginStatus?.Code,
            EndStatus = endStatus?.Code,
            Action = request.TransitionVerbPresentTense
        };
        
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
}