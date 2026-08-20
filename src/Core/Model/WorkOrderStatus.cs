using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ClearMeasure.Bootcamp.Core.Model;

[JsonConverter(typeof(WorkOrderStatusJsonConverter))]
public class WorkOrderStatus
{
    private static readonly ILogger _logger = NullLogger<WorkOrderStatus>.Instance;

    public static readonly WorkOrderStatus None = new("", "", " ", 0);
    public static readonly WorkOrderStatus Draft = new("DRT", "Draft", "Draft", 1);
    public static readonly WorkOrderStatus Assigned = new("ASD", "Assigned", "Assigned", 2);
    public static readonly WorkOrderStatus InProgress = new("IPG", "InProgress", "In Progress", 3);
    public static readonly WorkOrderStatus Complete = new("CMP", "Complete", "Complete", 4);
    public static readonly WorkOrderStatus Cancelled = new("CNL", "Cancelled", "Cancelled", 5);

    public WorkOrderStatus()
    {
        Code = null!;
        Key = null!;
        FriendlyName = null!;
    }

    protected WorkOrderStatus(string code, string key, string friendlyName, byte sortBy)
    {
        Code = code;
        Key = key;
        FriendlyName = friendlyName;
        SortBy = sortBy;
    }

    public static WorkOrderStatus[] GetAllItems()
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

    public override bool Equals(object? obj) =>
        obj is WorkOrderStatus status && IsSameCode(status);

    private bool IsSameCode(WorkOrderStatus status) =>
        GetType() == status.GetType() && Code.Equals(status.Code);

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

    public static WorkOrderStatus FromCode(string code)
    {
        var items = GetAllItems();
        var match =
            Array.Find(items, instance => instance.Code == code)!;

        return match;
    }

    public static WorkOrderStatus FromKey(string? key)
    {
        ArgumentNullException.ThrowIfNull(key);

        var match = Array.Find(GetAllItems(),
            instance => instance.Key.Equals(key, StringComparison.InvariantCultureIgnoreCase));

        return match ?? throw new ArgumentOutOfRangeException(
            nameof(key),
            $"Key '{key}' is not a valid key for {nameof(WorkOrderStatus)}");
    }

    public static WorkOrderStatus Parse(string? name)
    {
        return FromKey(name);
    }
}

public class WorkOrderStatusJsonConverter : JsonConverter<WorkOrderStatus>
{
    public override WorkOrderStatus Read(ref Utf8JsonReader reader, Type typeToConvert,
        JsonSerializerOptions options)
    {
        var key = reader.GetString();
        return WorkOrderStatus.FromKey(key);
    }

    public override void Write(Utf8JsonWriter writer, WorkOrderStatus value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Key);
    }
}