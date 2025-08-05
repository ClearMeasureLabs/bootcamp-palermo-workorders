using ClearMeasure.Bootcamp.Core;
using MediatR;

namespace ClearMeasure.Bootcamp.LlmGateway;

public record ChatClientConfigQuery : IRequest<ChatClientConfig>, IRemotableRequest
{
}