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
        var endStatus = request.WorkOrder.Status;

        var order = request.WorkOrder;
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

        // Create audit entry for the status change or edit
        var auditEntry = new AuditEntry
        {
            Id = Guid.NewGuid(),
            WorkOrderId = order.Id,
            EmployeeId = request.CurrentUser.Id,
            EmployeeName = request.CurrentUser.GetFullName(),
            EntryDate = time.GetUtcNow().DateTime,
            BeginStatus = beginStatus?.Code,
            EndStatus = endStatus?.Code,
            ActionType = request.TransitionVerbPresentTense,
            Description = $"{request.CurrentUser.GetFullName()} {request.TransitionVerbPastTense.ToLower()} work order {order.Number}"
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