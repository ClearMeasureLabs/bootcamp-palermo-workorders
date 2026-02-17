using MediatR;

namespace ClearMeasure.Bootcamp.Core;

public interface IBus
{
    Task<TResponse> Send<TResponse>(IRequest<TResponse> request);
    Task<object?> Send(object request);
    Task Publish(INotification notification);
}