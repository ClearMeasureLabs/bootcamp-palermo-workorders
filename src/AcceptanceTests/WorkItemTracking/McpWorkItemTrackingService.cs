using ClearMeasure.Bootcamp.AcceptanceTests.McpServer;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkItemTracking;

/// <summary>
/// <see cref="IWorkItemTrackingService"/> implementation that delegates to MCP tools on the connected <see cref="McpTestHelper"/>.
/// </summary>
public sealed class McpWorkItemTrackingService(McpTestHelper helper) : IWorkItemTrackingService
{
    public Task<string> CreateDraftWorkOrderAsync(string title, string description, string creatorUsername,
        string? roomNumber = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var args = new Dictionary<string, object?>
        {
            ["title"] = title,
            ["description"] = description,
            ["creatorUsername"] = creatorUsername
        };
        if (!string.IsNullOrEmpty(roomNumber))
        {
            args["roomNumber"] = roomNumber;
        }

        return helper.CallToolDirectly("create-work-order", args);
    }

    public Task<string> ExecuteWorkOrderCommandAsync(string workOrderNumber, string commandName,
        string executingUsername, string? assigneeUsername = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var args = new Dictionary<string, object?>
        {
            ["workOrderNumber"] = workOrderNumber,
            ["commandName"] = commandName,
            ["executingUsername"] = executingUsername
        };
        if (!string.IsNullOrEmpty(assigneeUsername))
        {
            args["assigneeUsername"] = assigneeUsername;
        }

        return helper.CallToolDirectly("execute-work-order-command", args);
    }

    public Task<string> GetWorkOrderAsync(string workOrderNumber, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return helper.CallToolDirectly("get-work-order",
            new Dictionary<string, object?> { ["workOrderNumber"] = workOrderNumber });
    }

    public Task<string> ListWorkOrdersAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return helper.CallToolDirectly("list-work-orders", new Dictionary<string, object?>());
    }

    public Task<string> ListEmployeesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return helper.CallToolDirectly("list-employees", new Dictionary<string, object?>());
    }

    public Task<string> GetEmployeeAsync(string username, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return helper.CallToolDirectly("get-employee",
            new Dictionary<string, object?> { ["username"] = username });
    }
}
