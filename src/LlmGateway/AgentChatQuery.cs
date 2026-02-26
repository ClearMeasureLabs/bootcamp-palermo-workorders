using ClearMeasure.Bootcamp.Core;
using MediatR;
using Microsoft.Extensions.AI;

namespace ClearMeasure.Bootcamp.LlmGateway;

public record AgentChatQuery(string Prompt, string UserName) : IRequest<ChatResponse>, IRemotableRequest
{
}
