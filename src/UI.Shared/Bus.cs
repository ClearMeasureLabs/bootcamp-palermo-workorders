using System.Collections;
using System.Diagnostics;
using ClearMeasure.Bootcamp.Core;
using MediatR;

namespace ClearMeasure.Bootcamp.UI.Shared;

public class Bus : IBus
{
    private static readonly ActivitySource ActivitySource = new("ChurchBulletin.Application.Bus", "1.0.0");

    private readonly IMediator _mediator;

    public Bus(IMediator mediator)
    {
        _mediator = mediator;
    }

    public virtual async Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
    {
        using var activity = StartActivity(request);

        try
        {
            return await _mediator.Send(request);
        }
        catch (Exception ex)
        {
            SetActivityError(activity, ex);
            throw;
        }
    }

    public virtual async Task<object?> Send(object request)
    {
        using var activity = StartActivity(request);

        try
        {
            return await _mediator.Send(request);
        }
        catch (Exception ex)
        {
            SetActivityError(activity, ex);
            throw;
        }
    }

    public virtual async Task Publish(INotification notification)
    {
        using var activity = StartActivity(notification, "Publish");

        try
        {
            await _mediator.Publish(notification);
        }
        catch (Exception ex)
        {
            SetActivityError(activity, ex);
            throw;
        }
    }

    private static Activity? StartActivity(object message, string operation = "Send")
    {
        var messageName = message.GetType().Name;
        var parentContext = Activity.Current?.Context;

        var activity = parentContext.HasValue
            ? ActivitySource.StartActivity($"Bus.{operation} {messageName}", ActivityKind.Internal, parentContext.Value)
            : ActivitySource.StartActivity($"Bus.{operation} {messageName}", ActivityKind.Internal);

        if (activity is null)
        {
            return null;
        }

        activity.SetTag("bus.operation", operation);
        activity.SetTag("bus.message.type", messageName);
        activity.SetTag("bus.message.fullname", message.GetType().FullName);
        AddScalarPropertyTags(message, activity);

        return activity;
    }

    private static void AddScalarPropertyTags(object message, Activity activity) =>
        BusActivityTagger.AddScalarPropertyTags(message, activity);

    private static void SetActivityError(Activity? activity, Exception ex)
    {
        if (activity is null) return;

        activity.SetStatus(ActivityStatusCode.Error, ex.Message);
        activity.SetTag("error", true);
        activity.SetTag("exception.type", ex.GetType().FullName);
        activity.SetTag("exception.message", ex.Message);
        activity.SetTag("exception.stacktrace", ex.ToString());
    }
}