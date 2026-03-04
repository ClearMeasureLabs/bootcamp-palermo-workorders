using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

public class AddSubtaskCommandHandler(DbContext dbContext) : IRequestHandler<AddSubtaskCommand, WorkOrderSubtask>
{
    public async Task<WorkOrderSubtask> Handle(AddSubtaskCommand request, CancellationToken cancellationToken = default)
    {
        WorkOrderSubtask.ValidateTitle(request.Title);

        var subtask = new WorkOrderSubtask
        {
            Id = Guid.NewGuid(),
            WorkOrderId = request.WorkOrderId,
            Title = request.Title,
            SortOrder = request.SortOrder,
            IsCompleted = false
        };

        dbContext.Add(subtask);
        await dbContext.SaveChangesAsync(cancellationToken);

        return subtask;
    }
}

public class ToggleSubtaskCommandHandler(DbContext dbContext) : IRequestHandler<ToggleSubtaskCommand, WorkOrderSubtask>
{
    public async Task<WorkOrderSubtask> Handle(ToggleSubtaskCommand request, CancellationToken cancellationToken = default)
    {
        var subtask = await dbContext.Set<WorkOrderSubtask>()
            .SingleAsync(s => s.Id == request.SubtaskId, cancellationToken);

        subtask.IsCompleted = !subtask.IsCompleted;
        await dbContext.SaveChangesAsync(cancellationToken);

        return subtask;
    }
}

public class RemoveSubtaskCommandHandler(DbContext dbContext) : IRequestHandler<RemoveSubtaskCommand, bool>
{
    public async Task<bool> Handle(RemoveSubtaskCommand request, CancellationToken cancellationToken = default)
    {
        var subtask = await dbContext.Set<WorkOrderSubtask>()
            .SingleOrDefaultAsync(s => s.Id == request.SubtaskId, cancellationToken);

        if (subtask == null) return false;

        dbContext.Remove(subtask);
        await dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}
