using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using MediatR;
using Microsoft.Extensions.AI;

namespace ClearMeasure.Bootcamp.LlmGateway;

public record WorkRequestChatQuery(string Prompt, WorkRequest CurrentWorkRequest) : IRequest<ChatResponse>, IRemotableRequest
{
}