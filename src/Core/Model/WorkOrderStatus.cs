using System.Text.Json;
using System.Text.Json.Serialization;

namespace ClearMeasure.Bootcamp.Core.Model;

[JsonConverter(typeof(WorkOrderStatusJsonConverter))]
public class WorkOrderStatus : IEquatable<WorkOrderStatus>
{
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

    public bool Equals(WorkOrderStatus? other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (GetType() != other.GetType())
        {
            return false;
        }

        return Code.Equals(other.Code);
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as WorkOrderStatus);
    }

    public override string ToString()
    {
        return FriendlyName;
    }

    public override int GetHashCode()
    {
        return Code.GetHashCode();
    }

    public static bool operator ==(WorkOrderStatus? left, WorkOrderStatus? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(WorkOrderStatus? left, WorkOrderStatus? right)
    {
        return !Equals(left, right);
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
        if (key == null)
        {
            throw new NotSupportedException("Finding a WorkOrderStatusCode for a null key is not supported");
        }

        var items = GetAllItems();
        var match = Array.Find(items,
            instance => instance.Key.Equals(key, StringComparison.InvariantCultureIgnoreCase))!;

        if (match == null)
        {
            throw new ArgumentOutOfRangeException(
                $"Key '{key}' is not a valid key for {nameof(WorkOrderStatus)}");
        }

        return match;
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