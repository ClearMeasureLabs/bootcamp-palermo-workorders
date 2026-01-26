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
        
        var order = request.WorkOrder;
        var beginStatus = order.Status;
        
        request.Execute(new StateCommandContext { CurrentDateTime = time.GetUtcNow().DateTime });

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
        
        // Save to get the WorkOrder Id generated
        await dbContext.SaveChangesAsync();
        
        // Create audit entry after WorkOrder has been saved and has an Id
        var auditEntry = CreateAuditEntry(order, request.CurrentUser, beginStatus, order.Status, time.GetUtcNow().DateTime);
        dbContext.Add(auditEntry);
        
        // Save again with the audit entry
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
    
    private AuditEntry CreateAuditEntry(WorkOrder workOrder, Employee employee, WorkOrderStatus beginStatus, WorkOrderStatus endStatus, DateTime date)
    {
        var sequence = GetNextSequence(workOrder.Id);
        
        return new AuditEntry
        {
            WorkOrderId = workOrder.Id,
            Sequence = sequence,
            EmployeeId = employee.Id,
            ArchivedEmployeeName = employee.GetFullName(),
            Date = date,
            BeginStatus = beginStatus,
            EndStatus = endStatus
        };
    }
    
    private int GetNextSequence(Guid workOrderId)
    {
        var maxSequence = dbContext.Set<AuditEntry>()
            .Where(ae => ae.WorkOrderId == workOrderId)
            .Select(ae => (int?)ae.Sequence)
            .Max() ?? 0;
            
        return maxSequence + 1;
    }
}