using MediatR;

namespace ClearMeasure.Bootcamp.Core.Model.StateCommands;

public record CreateDatedWorkOrdersCommand(
    string CreatorUsername,
    string AssigneeUsername,
    [property: ExcludeFromBusActivity] string Title,
    [property: ExcludeFromBusActivity] string Description,
    IReadOnlyList<DateOnly> DueDates) : IRequest<CreateDatedWorkOrdersResult>, IRemotableRequest
{
    public const int MaximumBatchSize = 10;
}

public record CreateDatedWorkOrdersResult(
    bool Success,
    string Message,
    IReadOnlyList<CreatedDatedWorkOrder> WorkOrders);

public record CreatedDatedWorkOrder(string Number, DateOnly DueDate);
