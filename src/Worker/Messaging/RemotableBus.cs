using System.Net.Http.Json;
using System.Text.Json;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Messaging;
using MediatR;
using Microsoft.Extensions.Http;

namespace Worker.Messaging;

/// <summary>
/// Sends <see cref="IRemotableRequest"/> and <see cref="IRemotableEvent"/> messages to the UI server API and
/// </summary>
public class RemotableBus(IHttpClientFactory httpClientFactory) : IBus
{
    /// <summary>
    /// Named client key for <see cref="IHttpClientFactory.CreateClient(string)"/> (see <c>AddHttpClient</c> registration).
    /// </summary>
    public const string HttpClientName = nameof(RemotableBus);

    public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
    {
        if (request is not IRemotableRequest remotableRequest)
        {
            throw new NotSupportedException($"Message type {request.GetType().Name} is not supported. Only IRemotableRequest is supported.");
        }

        var response = await PostMessage(remotableRequest);
        return (TResponse)response!.GetBodyObject();
    }

    public async Task<object?> Send(object request)
    {
        if (request is not IRemotableRequest remotableRequest)
        {
            throw new NotSupportedException($"Message type {request.GetType().Name} is not supported. Only IRemotableRequest is supported.");
        }

        var response = await PostMessage(remotableRequest);
        return response?.GetBodyObject();
    }

    public async Task Publish(INotification notification)
    {
        if (notification is not IRemotableEvent remotableEvent)
        {
            throw new NotSupportedException($"Message type {notification.GetType().Name} is not supported. Only IRemotableEvent is supported.");
        }

        await PostMessage(remotableEvent);
    }

    private async Task<WebServiceMessage?> PostMessage(object payload)
    {
        using var httpClient = httpClientFactory.CreateClient(HttpClientName);
        if (httpClient.BaseAddress is null)
        {
            throw new InvalidOperationException(
                $"HttpClient '{HttpClientName}' must have BaseAddress set (register with AddHttpClient and configure BaseAddress).");
        }

        var message = new WebServiceMessage(payload);
        var result = await httpClient.PostAsJsonAsync(string.Empty, message);
        var json = await result.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<WebServiceMessage>(json);
    }
}