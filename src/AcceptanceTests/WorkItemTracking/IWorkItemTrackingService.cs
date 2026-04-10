namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkItemTracking;

/// <summary>
/// Test-layer abstraction for work-order operations used by E2E tests so scenarios do not depend on MCP tool names or transport details.
/// </summary>
public interface IWorkItemTrackingService
{
    Task<string> CreateDraftWorkOrderAsync(string title, string description, string creatorUsername,
        string? roomNumber = null, CancellationToken cancellationToken = default);

    Task<string> ExecuteWorkOrderCommandAsync(string workOrderNumber, string commandName, string executingUsername,
        string? assigneeUsername = null, CancellationToken cancellationToken = default);

    Task<string> GetWorkOrderAsync(string workOrderNumber, CancellationToken cancellationToken = default);

    Task<string> ListWorkOrdersAsync(CancellationToken cancellationToken = default);

    Task<string> ListEmployeesAsync(CancellationToken cancellationToken = default);

    Task<string> GetEmployeeAsync(string username, CancellationToken cancellationToken = default);
}
