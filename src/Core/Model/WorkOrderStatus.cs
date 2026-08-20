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

    public override bool Equals(object? obj)
    {
        var other = obj as WorkOrderStatus;
        if (other == null)
        {
            return false;
        }

        if (GetType() != obj!.GetType())
        {
            return false;
        }

        // Delegate to operator== so both members agree on every combination of
        // null/non-null Code (see the operator's remarks below).
        return this == other;
    }

    public override string ToString()
    {
        return FriendlyName;
    }

    public override int GetHashCode()
    {
        // An instance with a null Code only exists briefly during EF Core /
        // serialization materialization (see the parameterless constructor) and is
        // never a legitimate domain value. Such instances are never equal to anything
        // but themselves (see operator==), so a fixed sentinel hash code here does not
        // violate the Equals/GetHashCode contract; it only means uninitialized
        // instances may collide with each other and with hash code 0, which is
        // harmless since they never compare equal by value.
        return Code is null ? 0 : Code.GetHashCode();
    }

    public static bool operator ==(WorkOrderStatus? left, WorkOrderStatus? right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        if (left is null || right is null)
        {
            return false;
        }

        if (left.GetType() != right.GetType())
        {
            return false;
        }

        if (left.Code is null || right.Code is null)
        {
            // An instance with a null Code only exists briefly during EF Core /
            // serialization materialization (see the parameterless constructor) and is
            // never a legitimate domain value. Two such instances are not the same
            // status just because both are uninitialized, so they never compare equal
            // unless they are the same reference (handled above).
            return false;
        }

        return string.Equals(left.Code, right.Code, StringComparison.Ordinal);
    }

    public static bool operator !=(WorkOrderStatus? left, WorkOrderStatus? right)
    {
        return !(left == right);
    }

    public bool IsEmpty()
    {
        // Code == "" is a string comparison, which string's own == operator handles
        // safely for a null Code (null == "" is simply false, no exception). A
        // null-Code (uninitialized) instance therefore correctly reports IsEmpty() as
        // false rather than throwing - it is not "empty" in the None-singleton sense,
        // it is transiently uninitialized. No change needed here.
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