using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.DataAccess.Mappings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

/// <summary>Handles creating a work order pre-populated from an existing template.</summary>
public class CreateWorkOrderFromTemplateCommandHandler(
    DataContext context,
    IWorkOrderNumberGenerator numberGenerator,
    TimeProvider time)
    : IRequestHandler<CreateWorkOrderFromTemplateCommand, WorkOrder>
{
    public async Task<WorkOrder> Handle(CreateWorkOrderFromTemplateCommand request,
        CancellationToken cancellationToken = default)
    {
        var template = await context.Set<WorkOrderTemplate>()
            .SingleAsync(t => t.Id == request.TemplateId, cancellationToken);

        var creator = await context.Set<Employee>()
            .Include("Roles")
            .SingleAsync(e => e.Id == request.CreatorId, cancellationToken);

        var workOrder = template.ToWorkOrder(creator, numberGenerator.GenerateNumber(), time.GetUtcNow().DateTime);

        context.Add(workOrder);
        await context.SaveChangesAsync(cancellationToken);
        return workOrder;
    }
}
