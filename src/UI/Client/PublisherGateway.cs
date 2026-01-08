using System.Net.Http.Json;
using System.Text.Json;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Exceptions;

namespace ClearMeasure.Bootcamp.UI.Client;

public class PublisherGateway(HttpClient httpClient) : IPublisherGateway
{
    public const string ApiRelativeUrl = "api/blazor-wasm-single-api";

    public async Task<WebServiceMessage?> Publish(IRemotableRequest request)
    {
        var message = new WebServiceMessage(request);
        return await SendToTopic(message);
    }

    public virtual async Task<WebServiceMessage?> SendToTopic(WebServiceMessage message)
    {
        HttpContent content = new StringContent(message.GetJson());
        var result = await httpClient.PostAsJsonAsync(ApiRelativeUrl, message);
        
        if (!result.IsSuccessStatusCode)
        {
            var errorJson = await result.Content.ReadAsStringAsync();
            
            if (result.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                try
                {
                    var errorResponse = JsonSerializer.Deserialize<ValidationErrorResponse>(errorJson, 
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    if (errorResponse?.Errors != null)
                    {
                        throw new ValidationException(errorResponse.Errors);
                    }
                }
                catch (JsonException)
                {
                    // If parsing fails, throw generic exception
                }
            }
            
            result.EnsureSuccessStatusCode();
        }
        
        var json = await result.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<WebServiceMessage>(json);
    }
}

internal class ValidationErrorResponse
{
    public Dictionary<string, string[]>? Errors { get; set; }
}