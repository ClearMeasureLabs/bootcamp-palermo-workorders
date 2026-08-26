using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

/// <summary>
/// Persists work-order state commands. Existing rows are loaded and updated in place so
/// EF change tracking compares against database originals (Clear+Attach+Update left
/// shadow FKs like AssigneeId unmodified when the navigation was null on the detached graph).
/// </summary>
public class StateCommandHandler(
    DbContext dbContext,
    TimeProvider time,
    IDistributedBus distributedBus,
    ILogger<StateCommandHandler> logger)
    : IRequestHandler<StateCommandBase, StateCommandResult>
{
    public async Task<StateCommandResult> Handle(StateCommandBase request,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Executing");
        request.Execute(new StateCommandContext { CurrentDateTime = time.GetUtcNow().DateTime });

        var order = request.WorkOrder;
        if (order.Assignee == order.Creator)
        {
            order.Assignee = order.Creator; //EFCore reference checking
        }

        var persisted = await PersistAsync(order, cancellationToken);

        var loweredTransitionVerb = request.TransitionVerbPastTense.ToLower();
        var workOrderNumber = persisted.Number;
        var fullName = request.CurrentUser.GetFullName();

        var debugMessage = $"{fullName} has {loweredTransitionVerb} work order {workOrderNumber}";
        logger.LogDebug(debugMessage);
        logger.LogInformation("Executed");

        var result = new StateCommandResult(persisted, request.TransitionVerbPresentTense, debugMessage);

        await distributedBus.PublishAsync(request.StateTransitionEvent, cancellationToken);
        return result;
    }

    private async Task<WorkOrder> PersistAsync(WorkOrder order, CancellationToken cancellationToken)
    {
        if (order.Id == Guid.Empty)
        {
            return await InsertNewAsync(order, cancellationToken);
        }

        return await UpdateExistingAsync(order, cancellationToken);
    }

    private async Task<WorkOrder> InsertNewAsync(WorkOrder order, CancellationToken cancellationToken)
    {
        dbContext.ChangeTracker.Clear();
        dbContext.Attach(order);
        dbContext.Add(order);
        await dbContext.SaveChangesAsync(cancellationToken);
        return order;
    }

    private async Task<WorkOrder> UpdateExistingAsync(WorkOrder order, CancellationToken cancellationToken)
    {
        dbContext.ChangeTracker.Clear();

        var existing = await dbContext.Set<WorkOrder>()
            .SingleAsync(workOrder => workOrder.Id == order.Id, cancellationToken);

        ApplyScalarValues(existing, order);
        await ApplyRelationshipsAsync(existing, order, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
        return existing;
    }

    private static void ApplyScalarValues(WorkOrder target, WorkOrder source)
    {
        target.Number = source.Number;
        target.Title = source.Title;
        target.Description = source.Description;
        target.Instructions = source.Instructions;
        target.RoomNumber = source.RoomNumber;
        target.Status = source.Status;
        target.AssignedDate = source.AssignedDate;
        target.CreatedDate = source.CreatedDate;
        target.CompletedDate = source.CompletedDate;
        target.DueDate = source.DueDate;
    }

    private async Task ApplyRelationshipsAsync(
        WorkOrder target,
        WorkOrder source,
        CancellationToken cancellationToken)
    {
        if (source.Creator != null)
        {
            target.Creator = await ResolveEmployeeAsync(source.Creator.Id, cancellationToken);
        }

        target.Assignee = source.Assignee == null
            ? null
            : await ResolveEmployeeAsync(source.Assignee.Id, cancellationToken);
    }

    private async Task<Employee> ResolveEmployeeAsync(Guid employeeId, CancellationToken cancellationToken)
    {
        var tracked = dbContext.Set<Employee>().Local
            .FirstOrDefault(employee => employee.Id == employeeId);
        if (tracked != null)
        {
            return tracked;
        }

        return await dbContext.Set<Employee>()
            .SingleAsync(employee => employee.Id == employeeId, cancellationToken);
    }
}
