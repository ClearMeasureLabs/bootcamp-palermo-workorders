using System.Net.Http.Json;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using MediatR;

namespace ClearMeasure.Bootcamp.UI.Server;

/// <summary>
/// Posts a JSON webhook after Draft→Assigned when <c>Webhooks:WorkOrderAssignedUrl</c> is configured.
/// </summary>
public class WorkOrderAssignedWebhookHandler(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<WorkOrderAssignedWebhookHandler> logger)
    : IPipelineBehavior<DraftToAssignedCommand, StateCommandResult>
{
    /// <summary>Configuration key for the outbound webhook URL.</summary>
    public const string ConfigKey = "Webhooks:WorkOrderAssignedUrl";

    /// <inheritdoc />
    public async Task<StateCommandResult> Handle(
        DraftToAssignedCommand request,
        RequestHandlerDelegate<StateCommandResult> next,
        CancellationToken cancellationToken)
    {
        var result = await next();
        await NotifyAsync(result.WorkOrder, cancellationToken);
        return result;
    }

    /// <summary>
    /// POSTs {number,title,assignee} when the webhook URL is non-empty; skips otherwise.
    /// Failures are logged and not thrown.
    /// </summary>
    public async Task NotifyAsync(WorkOrder workOrder, CancellationToken cancellationToken = default)
    {
        var url = configuration[ConfigKey];
        if (string.IsNullOrWhiteSpace(url))
        {
            return;
        }

        try
        {
            var client = httpClientFactory.CreateClient();
            var payload = new
            {
                number = workOrder.Number,
                title = workOrder.Title,
                assignee = workOrder.Assignee?.UserName
            };
            using var response = await client.PostAsJsonAsync(url, payload, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Work order assigned webhook returned {StatusCode} for {WorkOrderNumber}",
                    (int)response.StatusCode,
                    workOrder.Number);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Work order assigned webhook failed for {WorkOrderNumber}", workOrder.Number);
        }
    }
}
