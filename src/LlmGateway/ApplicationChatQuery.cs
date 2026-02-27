using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using MediatR;
using Microsoft.Extensions.AI;

namespace ClearMeasure.Bootcamp.LlmGateway;

public record ApplicationChatQuery(string Prompt, string CurrentUsername) : IRequest<ChatResponse>, IRemotableRequest
{
    public List<ChatHistoryMessage> ChatHistory { get; set; } = [];
}

public record ChatHistoryMessage(string Role, string Content);
