using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.Events;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ClearMeasure.Bootcamp.DataAccess.Handlers;

/// <summary>
/// Persists multiple dated assigned work orders in one database transaction.
/// </summary>
public class CreateDatedWorkOrdersHandler(
    DbContext dbContext,
    IBus bus,
    IWorkOrderNumberGenerator numberGenerator,
    TimeProvider time,
    ILogger<CreateDatedWorkOrdersHandler> logger)
    : IRequestHandler<CreateDatedWorkOrdersCommand, CreateDatedWorkOrdersResult>
{
    public async Task<CreateDatedWorkOrdersResult> Handle(
        CreateDatedWorkOrdersCommand request,
        CancellationToken cancellationToken)
    {
        if (request.DueDates.Count == 0)
        {
            return new CreateDatedWorkOrdersResult(false, "At least one due date is required.", []);
        }

        var assignee = await FindEmployeeAsync(request.AssigneeUsername, cancellationToken);
        if (assignee is null)
        {
            logger.LogWarning("Assignee '{AssigneeUsername}' not found; creating zero work orders",
                request.AssigneeUsername);
            return new CreateDatedWorkOrdersResult(
                false,
                $"Assignee '{request.AssigneeUsername}' was not found. No work orders were created.",
                []);
        }

        var creator = await FindEmployeeAsync(request.CreatorUsername, cancellationToken);
        if (creator is null)
        {
            logger.LogWarning("Creator '{CreatorUsername}' not found; creating zero work orders",
                request.CreatorUsername);
            return new CreateDatedWorkOrdersResult(
                false,
                $"Creator '{request.CreatorUsername}' was not found. No work orders were created.",
                []);
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var now = time.GetUtcNow().DateTime;
            var created = new List<CreatedDatedWorkOrder>(request.DueDates.Count);

            foreach (var dueDate in request.DueDates)
            {
                var workOrder = new WorkOrder
                {
                    Number = numberGenerator.GenerateNumber(),
                    Title = request.Title,
                    Description = request.Description,
                    Creator = creator,
                    Assignee = assignee,
                    Status = WorkOrderStatus.Assigned,
                    CreatedDate = now,
                    AssignedDate = now,
                    DueDate = dueDate
                };

                dbContext.Add(workOrder);
                created.Add(new CreatedDatedWorkOrder(workOrder.Number!, dueDate));
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            await bus.Publish(new DatedWorkOrdersCreatedEvent(created.Count));

            logger.LogInformation(
                "Created {Count} dated work orders for assignee {AssigneeUsername}",
                created.Count,
                request.AssigneeUsername);

            return new CreateDatedWorkOrdersResult(
                true,
                $"Created {created.Count} work orders.",
                created);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed creating dated work orders; rolling back");
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task<Employee?> FindEmployeeAsync(string username, CancellationToken cancellationToken)
    {
        return await dbContext.Set<Employee>()
            .Include(e => e.Roles)
            .SingleOrDefaultAsync(e => e.UserName == username, cancellationToken);
    }
}
