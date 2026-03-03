using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

public class ClearContextCommandHandler(DbContext context) : IRequestHandler<ClearContextCommand>
{
    private readonly DbContext context = context;

    Task IRequestHandler<ClearContextCommand>.Handle(ClearContextCommand request, CancellationToken cancellationToken)
    {
        context.ChangeTracker.Clear();
        return Task.CompletedTask;
    }
}