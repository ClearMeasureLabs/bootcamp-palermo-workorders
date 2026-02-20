using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ClearMeasure.Bootcamp.Core;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

public class StateCommandHandler(DbContext dbContext, TimeProvider time, IDistributedBus distributedBus, ILogger<StateCommandHandler> logger)
    : IRequestHandler<StateCommandBase, StateCommandResult>
{
    public async Task<StateCommandResult> Handle(StateCommandBase request,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Executing");
        
        // Validate required fields
        if (string.IsNullOrWhiteSpace(request.WorkOrder.Title))
        {
            throw new InvalidOperationException("Title is required");
        }
        
        if (string.IsNullOrWhiteSpace(request.WorkOrder.Description))
        {
            throw new InvalidOperationException("Description is required");
        }
        
        request.Execute(new StateCommandContext { CurrentDateTime = time.GetUtcNow().DateTime });

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

        var loweredTransitionVerb = request.TransitionVerbPastTense.ToLower();
        var workOrderNumber = order.Number;
        var fullName = request.CurrentUser.GetFullName();

        var debugMessage = string.Format("{0} has {1} work order {2}", fullName, loweredTransitionVerb, workOrderNumber);
        logger.LogDebug(debugMessage);
        logger.LogInformation("Executed");

        var result = new StateCommandResult(order, request.TransitionVerbPresentTense, debugMessage);

        await distributedBus.PublishAsync(request.StateTransitionEvent, cancellationToken);
        return result;
    }
}