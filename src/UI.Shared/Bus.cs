using System.Collections;
using System.Diagnostics;
using ClearMeasure.Bootcamp.Core;
using MediatR;

namespace ClearMeasure.Bootcamp.UI.Shared;

public class Bus : IBus
{
    private static readonly ActivitySource BusActivitySource = new(TelemetryConstants.ApplicationSourceName);
    private readonly IMediator _mediator;

    public Bus(IMediator mediator)
    {
        _mediator = mediator;
    }

    public virtual async Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
    {
        using var activity = BusActivitySource.StartActivity($"Bus.Send:{request.GetType().Name}");
        AddActivityTags(activity, request);

        var response = await _mediator.Send(request);
        return response;
    }

    public virtual async Task<object?> Send(object request)
    {
        using var activity = BusActivitySource.StartActivity($"Bus.Send:{request.GetType().Name}");
        AddActivityTags(activity, request);

        var response = await _mediator.Send(request);
        return response;
    }

    public void Publish<TNotification>(TNotification notification) where TNotification : INotification
    {
        _mediator.Publish(notification);
    }

    private static void AddActivityTags(Activity? activity, object message)
    {
        if (activity == null) return;

        activity.SetTag("bus.message.type", message.GetType().Name);
        activity.SetTag("bus.message.fullname", message.GetType().FullName);

        var properties = message.GetType().GetProperties();
        foreach (var property in properties)
        {
            if (!typeof(IEnumerable).IsAssignableFrom(property.PropertyType) || property.PropertyType == typeof(string))
            {
                var propertyValue = property.GetValue(message);
                activity.SetTag($"bus.message.{property.Name}", propertyValue?.ToString() ?? string.Empty);
            }
        }
    }
}