using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.LlmGateway;
using Microsoft.Extensions.AI;

namespace ClearMeasure.Bootcamp.UI.Server;

/// <summary>
///     AI agent responsible for reformatting work request title and description fields
/// </summary>
public class WorkRequestReformatAgent(
    ChatClientFactory chatClientFactory,
    ILogger<WorkRequestReformatAgent> logger)
{
    /// <summary>
    ///     Reformats a work request's description for grammar and punctuation,
    ///     and ensures the title starts with a capital letter.
    ///     Returns the updated title and description, or null if no changes are needed.
    /// </summary>
    public async Task<ReformatResult?> ReformatWorkRequestAsync(WorkRequest workRequest)
    {
        try
        {
            var chatClient = await chatClientFactory.GetChatClient();

            var systemPrompt = """
                               You are an AI agent responsible for reformatting work request fields.
                               You will receive a work request title and description.

                               Your tasks:
                               1. Correct the description for grammar and punctuation. Do not change the meaning.
                               2. Ensure the title starts with a capital letter. Do not change anything else about the title.

                               If no changes are needed, respond with exactly: NO_CHANGES

                               Otherwise respond in this exact format (two lines only):
                               TITLE: <corrected title>
                               DESCRIPTION: <corrected description>
                               """;

            var workRequestInfo = $"""
                                 Title: {workRequest.Title}
                                 Description: {workRequest.Description}
                                 """;

            var messages = new List<ChatMessage>
            {
                new(ChatRole.System, systemPrompt),
                new(ChatRole.User, workRequestInfo)
            };

            var response = await chatClient.GetResponseAsync(messages);
            var responseText = response.Text?.Trim();

            if (string.IsNullOrWhiteSpace(responseText) ||
                responseText.Equals("NO_CHANGES", StringComparison.OrdinalIgnoreCase))
            {
                logger.LogInformation("No reformatting needed for WorkRequest {WorkRequestNumber}",
                    workRequest.Number);
                return null;
            }

            var result = ParseResponse(responseText, workRequest);

            if (result != null)
            {
                logger.LogInformation(
                    "Reformatted WorkRequest {WorkRequestNumber}: Title changed={TitleChanged}, Description changed={DescriptionChanged}",
                    workRequest.Number,
                    result.Title != workRequest.Title,
                    result.Description != workRequest.Description);
            }

            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error reformatting WorkRequest {WorkRequestNumber}",
                workRequest.Number);
            return null;
        }
    }

    internal static ReformatResult? ParseResponse(string responseText, WorkRequest workRequest)
    {
        var lines = responseText.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        string? title = null;
        string? description = null;

        foreach (var line in lines)
        {
            if (line.StartsWith("TITLE:", StringComparison.OrdinalIgnoreCase))
            {
                title = line["TITLE:".Length..].Trim();
            }
            else if (line.StartsWith("DESCRIPTION:", StringComparison.OrdinalIgnoreCase))
            {
                description = line["DESCRIPTION:".Length..].Trim();
            }
        }

        title ??= workRequest.Title;
        description ??= workRequest.Description;

        if (title == workRequest.Title && description == workRequest.Description)
        {
            return null;
        }

        return new ReformatResult(title!, description!);
    }
}

public record ReformatResult(string Title, string Description);
