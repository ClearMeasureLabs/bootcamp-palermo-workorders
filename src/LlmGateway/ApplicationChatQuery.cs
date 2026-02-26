using ClearMeasure.Bootcamp.Core;
using MediatR;
using Microsoft.Extensions.AI;

namespace ClearMeasure.Bootcamp.LlmGateway;

public record ApplicationChatQuery(string Prompt) : IRequest<ChatResponse>, IRemotableRequest
{
}
