using MediatR;
using Microsoft.Extensions.AI;

namespace ClearMeasure.Bootcamp.LlmGateway;

public record WorkOrderChatQuery(string Prompt) : IRequest<ChatResponse>
{
}