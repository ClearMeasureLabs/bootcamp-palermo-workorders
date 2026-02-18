using ClearMeasure.Bootcamp.Core.Model.Events;
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
    private const string AiBotRole = "Bot";

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

        if (order.Assignee?.Roles is null || string.IsNullOrWhiteSpace(request.TransitionVerbPastTense))
        {
            return result;
        }

        var isAssignedToBot = request.TransitionVerbPastTense.Equals("assigned", StringComparison.InvariantCultureIgnoreCase) &&
                              order.Assignee.Roles.Select(x => x.Name).Any(role => role == AiBotRole);

        if (isAssignedToBot)
        {
            var @event = new WorkOrderAssignedToBotEvent(request.CorrelationId, order.Id, order.Assignee!.Id);
            await distributedBus.PublishAsync(@event, cancellationToken);
        }

        return result;
    }
}