using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ClearMeasure.Bootcamp.Core.Model;

[JsonConverter(typeof(WorkRequestStatusJsonConverter))]
public class WorkRequestStatus
{
    private static readonly ILogger _logger = NullLogger<WorkRequestStatus>.Instance;

    public static readonly WorkRequestStatus None = new("", "", " ", 0);
    public static readonly WorkRequestStatus Draft = new("DRT", "Draft", "Draft", 1);
    public static readonly WorkRequestStatus Assigned = new("ASD", "Assigned", "Assigned", 2);
    public static readonly WorkRequestStatus InProgress = new("IPG", "InProgress", "In Progress", 3);
    public static readonly WorkRequestStatus Complete = new("CMP", "Complete", "Complete", 4);
    public static readonly WorkRequestStatus Cancelled = new("CNL", "Cancelled", "Cancelled", 5);

    public WorkRequestStatus()
    {
        Code = null!;
        Key = null!;
        FriendlyName = null!;
    }

    protected WorkRequestStatus(string code, string key, string friendlyName, byte sortBy)
    {
        Code = code;
        Key = key;
        FriendlyName = friendlyName;
        SortBy = sortBy;
    }

    public static WorkRequestStatus[] GetAllItems()
    {
        return new[]
        {
            Draft,
            Assigned,
            InProgress,
            Complete,
            Cancelled
        };
    }

    public string Code { get; }

    public string Key { get; }

    public string FriendlyName { get; set; }

    public byte SortBy { get; set; }

    public override bool Equals(object? obj)
    {
        var code = obj as WorkRequestStatus;
        if (code == null)
        {
            return false;
        }

        if (GetType() != obj!.GetType())
        {
            return false;
        }

        return Code.Equals(code.Code);
    }

    public override string ToString()
    {
        return FriendlyName;
    }

    public override int GetHashCode()
    {
        return Code.GetHashCode();
    }

    public bool IsEmpty()
    {
        return Code == "";
    }

    public static WorkRequestStatus FromCode(string code)
    {
        var items = GetAllItems();
        var match =
            Array.Find(items, instance => instance.Code == code)!;

        return match;
    }

    public static WorkRequestStatus FromKey(string? key)
    {
        if (key == null)
        {
            throw new NotSupportedException("Finding a WorkRequestStatusCode for a null key is not supported");
        }

        var items = GetAllItems();
        var match = Array.Find(items,
            instance => instance.Key.Equals(key, StringComparison.InvariantCultureIgnoreCase))!;

        if (match == null)
        {
            throw new ArgumentOutOfRangeException(
                $"Key '{key}' is not a valid key for {nameof(WorkRequestStatus)}");
        }

        return match;
    }

    public static WorkRequestStatus Parse(string? name)
    {
        return FromKey(name);
    }
}

public class WorkRequestStatusJsonConverter : JsonConverter<WorkRequestStatus>
{
    public override WorkRequestStatus Read(ref Utf8JsonReader reader, Type typeToConvert,
        JsonSerializerOptions options)
    {
        var key = reader.GetString();
        return WorkRequestStatus.FromKey(key);
    }

    public override void Write(Utf8JsonWriter writer, WorkRequestStatus value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Key);
    }
}