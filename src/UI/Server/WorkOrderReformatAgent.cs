using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.LlmGateway;
using Microsoft.Extensions.AI;

namespace ClearMeasure.Bootcamp.UI.Server;

public class WorkOrderReformatAgent(
    ChatClientFactory chatClientFactory,
    ILogger<WorkOrderReformatAgent> logger)
{
    public Task<ReformatResult?> ReformatWorkOrderAsync(WorkOrder workOrder) =>
        ReformatWorkOrderRunner.RunAsync(chatClientFactory, logger, workOrder);

    internal static ReformatResult? ParseResponse(string responseText, WorkOrder workOrder)
    {
        var title = ReformatResponseLineParser.ReadTitle(responseText, workOrder.Title ?? string.Empty);
        var description = ReformatResponseLineParser.ReadDescription(responseText, workOrder.Description ?? string.Empty);
        return title == (workOrder.Title ?? string.Empty)
            && description == (workOrder.Description ?? string.Empty)
            ? null
            : new ReformatResult(title, description);
    }
}

internal static class ReformatWorkOrderRunner
{
    internal static async Task<ReformatResult?> RunAsync(
        ChatClientFactory chatClientFactory,
        ILogger<WorkOrderReformatAgent> logger,
        WorkOrder workOrder)
    {
        try
        {
            var responseText = await RequestReformatTextAsync(chatClientFactory, workOrder);
            if (string.IsNullOrWhiteSpace(responseText)
                || responseText.Equals("NO_CHANGES", StringComparison.OrdinalIgnoreCase))
            {
                logger.LogInformation("No reformatting needed for WorkOrder {WorkOrderNumber}", workOrder.Number);
                return null;
            }

            var result = WorkOrderReformatAgent.ParseResponse(responseText, workOrder);
            if (result != null)
            {
                logger.LogInformation(
                    "Reformatted WorkOrder {WorkOrderNumber}: Title changed={TitleChanged}, Description changed={DescriptionChanged}",
                    workOrder.Number,
                    result.Title != workOrder.Title,
                    result.Description != workOrder.Description);
            }

            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error reformatting WorkOrder {WorkOrderNumber}", workOrder.Number);
            return null;
        }
    }

    private static async Task<string?> RequestReformatTextAsync(ChatClientFactory chatClientFactory, WorkOrder workOrder)
    {
        var chatClient = await chatClientFactory.GetChatClient();
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, ReformatPrompts.System),
            new(ChatRole.User, ReformatPrompts.ForWorkOrder(workOrder))
        };
        var response = await chatClient.GetResponseAsync(messages);
        return response.Text?.Trim();
    }
}

internal static class ReformatResponseLineParser
{
    internal static string ReadTitle(string responseText, string defaultTitle)
    {
        foreach (var line in responseText.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            if (TryParseTitle(line, out var title))
            {
                return title;
            }
        }

        return defaultTitle;
    }

    internal static string ReadDescription(string responseText, string defaultDescription)
    {
        foreach (var line in responseText.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            if (TryParseDescription(line, out var description))
            {
                return description;
            }
        }

        return defaultDescription;
    }

    internal static bool TryParseTitle(string line, out string value) =>
        TryParsePrefixedLine(line, "TITLE:", out value);

    internal static bool TryParseDescription(string line, out string value) =>
        TryParsePrefixedLine(line, "DESCRIPTION:", out value);

    private static bool TryParsePrefixedLine(string line, string prefix, out string value)
    {
        value = string.Empty;
        if (!line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        value = line[prefix.Length..].Trim();
        return true;
    }
}

internal static class ReformatPrompts
{
    internal const string System = """
                                   You are an AI agent responsible for reformatting work order fields.
                                   You will receive a work order title and description.

                                   Your tasks:
                                   1. Correct the description for grammar and punctuation. Do not change the meaning.
                                   2. Ensure the title starts with a capital letter. Do not change anything else about the title.

                                   If no changes are needed, respond with exactly: NO_CHANGES

                                   Otherwise respond in this exact format (two lines only):
                                   TITLE: <corrected title>
                                   DESCRIPTION: <corrected description>
                                   """;

    internal static string ForWorkOrder(WorkOrder workOrder) =>
        $"""
         Title: {workOrder.Title}
         Description: {workOrder.Description}
         """;
}

public record ReformatResult(string Title, string Description);
