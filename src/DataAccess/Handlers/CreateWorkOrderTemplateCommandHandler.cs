using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using MediatR;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

/// <summary>Handles the command that creates a new work order template.</summary>
public class CreateWorkOrderTemplateCommandHandler(DataContext context, TimeProvider time)
    : IRequestHandler<CreateWorkOrderTemplateCommand, WorkOrderTemplate>
{
    public async Task<WorkOrderTemplate> Handle(CreateWorkOrderTemplateCommand request,
        CancellationToken cancellationToken = default)
    {
        var template = new WorkOrderTemplate
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Description = request.Description,
            RoomNumber = request.RoomNumber,
            IsActive = true,
            CreatedById = request.CreatedById,
            CreatedDate = time.GetUtcNow().DateTime
        };

        context.Add(template);
        await context.SaveChangesAsync(cancellationToken);
        return template;
    }
}
