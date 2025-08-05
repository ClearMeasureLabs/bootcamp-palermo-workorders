using System.Collections;
using ClearMeasure.Bootcamp.Core;
using MediatR;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;

namespace ClearMeasure.Bootcamp.UI.Shared;

public class Bus : IBus
{
    private readonly TelemetryClient? _telemetryClient = null;
    private readonly IMediator _mediator;

    public Bus(IMediator mediator) : this(mediator, null)
    {

    }
    public Bus(IMediator mediator, TelemetryClient? telemetryClient)
    {
        _mediator = mediator;
        _telemetryClient = telemetryClient;
    }

    public virtual async Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
    {
        var response = await _mediator.Send(request);
        RecordCustomEvent(request);
        RecordTrace(request);
        return response;
    }

    public virtual async Task<object?> Send(object request)
    {
        var response = await _mediator.Send(request);
        return response;
    }

    public void Publish<TNotification>(TNotification notification) where TNotification : INotification
    {
        _mediator.Publish(notification);
    }

    private void RecordTrace(object message)
    {
        _telemetryClient?.TrackTrace(message.GetType().Name + ":- " + message, SeverityLevel.Verbose);
    }

    private void RecordCustomEvent(object message)
    {
        var telemetry = new EventTelemetry();
        telemetry.Name = message.GetType().Name;
        telemetry.Properties.Add("FullName", message.GetType().FullName);
        AddPropertyValues(message, telemetry.Properties);
        _telemetryClient?.TrackEvent(telemetry);
    }

    protected virtual void AddPropertyValues(object message, IDictionary<string, string> eventProperties)
    {
        var properties = message.GetType().GetProperties();
        foreach (var property in properties)
        {
            if (!typeof(IEnumerable).IsAssignableFrom(property.PropertyType) || property.PropertyType == typeof(string))
            {
                var propertyValue = property.GetValue(message);
                eventProperties.Add(property.Name, propertyValue?.ToString() ?? string.Empty);
            }
        }
    }
}