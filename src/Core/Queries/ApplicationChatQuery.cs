// ReSharper disable PropertyCanBeMadeInitOnly.Global -- Qodana P5 (#9440): NHibernate proxy / System.Text.Json set-by-convention requires mutable setters

using MediatR;

namespace ClearMeasure.Bootcamp.Core.Queries;

public record ApplicationChatQuery(
    [property: ExcludeFromBusActivity] string Prompt,
    string CurrentUsername)
    : IRequest<ApplicationChatResult>, IRemotableRequest
{
    public List<ChatHistoryMessage> ChatHistory { get; init; } = [];
}

public record ApplicationChatResult(string Text);

public record ChatHistoryMessage(string Role, string Content);
