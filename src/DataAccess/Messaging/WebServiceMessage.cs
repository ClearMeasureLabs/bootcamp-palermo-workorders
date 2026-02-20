using System.Text.Json;

namespace ClearMeasure.Bootcamp.DataAccess.Messaging;

public class WebServiceMessage
{
    private readonly JsonSerializerOptions _jsonSerializerOptions =
        new(JsonSerializerDefaults.General) { IncludeFields = false };

    public string Body { get; set; } = "";
    public string TypeName { get; set; } = "";

    public WebServiceMessage()
    {
    }

    public WebServiceMessage(object request)
    {
        Body = GetBody(request);
        TypeName = request.GetType().FullName + ", " + request.GetType().Assembly.GetName().Name;
    }

    public string GetBody(object request)
    {
        var body = JsonSerializer.Serialize(request, request.GetType(),
            _jsonSerializerOptions);
        return body;
    }

    public string GetJson()
    {
        return JsonSerializer.Serialize(this, GetType(), _jsonSerializerOptions);
    }

    public object GetBodyObject()
    {
        var type = Type.GetType(TypeName, true)!;
        var value = Body;
        return JsonSerializer.Deserialize(value, type, _jsonSerializerOptions)!;
    }
}