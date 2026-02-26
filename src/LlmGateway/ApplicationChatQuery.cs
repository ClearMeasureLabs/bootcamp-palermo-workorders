using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using MediatR;
using Microsoft.Extensions.AI;

namespace ClearMeasure.Bootcamp.LlmGateway;

public record ApplicationChatQuery(string Prompt, Employee CurrentUser) : IRequest<ChatResponse>, IRemotableRequest
{
    public Employee CurrentUser { get; set; } = CurrentUser;
}
