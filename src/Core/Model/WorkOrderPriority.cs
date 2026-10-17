using System.Text.Json;
using System.Text.Json.Serialization;

namespace ClearMeasure.Bootcamp.Core.Model;

/// <summary>
/// Work-order priority value list persisted as NVARCHAR (Low / Normal / High).
/// </summary>
[JsonConverter(typeof(WorkOrderPriorityJsonConverter))]
public class WorkOrderPriority : IEquatable<WorkOrderPriority>
{
    public static readonly WorkOrderPriority Low = new("Low");
    public static readonly WorkOrderPriority Normal = new("Normal");
    public static readonly WorkOrderPriority High = new("High");

    public WorkOrderPriority()
    {
        Key = null!;
    }

    protected WorkOrderPriority(string key)
    {
        Key = key;
    }

    public string Key { get; }

    public static WorkOrderPriority[] GetAllItems() => [Low, Normal, High];

    public static WorkOrderPriority FromKey(string? key)
    {
        ArgumentNullException.ThrowIfNull(key);

        var match = Array.Find(GetAllItems(),
            instance => instance.Key.Equals(key, StringComparison.OrdinalIgnoreCase));

        return match ?? throw new ArgumentOutOfRangeException(
            nameof(key),
            $"Key '{key}' is not a valid key for {nameof(WorkOrderPriority)}");
    }

    public bool Equals(WorkOrderPriority? other) =>
        ReferenceEquals(this, other) || HasSameKey(other);

    private bool HasSameKey(WorkOrderPriority? other) =>
        other is not null
        && GetType() == other.GetType()
        && KeysEqual(other.Key);

    private bool KeysEqual(string? otherKey) =>
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        Key is not null && string.Equals(Key, otherKey, StringComparison.Ordinal);

    public override bool Equals(object? obj) => Equals(obj as WorkOrderPriority);

    public override int GetHashCode()
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        return Key is null ? 0 : Key.GetHashCode();
    }

    public override string ToString() => Key;

    public static bool operator ==(WorkOrderPriority? left, WorkOrderPriority? right) =>
        left?.Equals(right) ?? right is null;

    public static bool operator !=(WorkOrderPriority? left, WorkOrderPriority? right) =>
        !(left == right);
}

public class WorkOrderPriorityJsonConverter : JsonConverter<WorkOrderPriority>
{
    public override WorkOrderPriority Read(ref Utf8JsonReader reader, Type typeToConvert,
        JsonSerializerOptions options)
    {
        var key = reader.GetString();
        return WorkOrderPriority.FromKey(key);
    }

    public override void Write(Utf8JsonWriter writer, WorkOrderPriority value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Key);
    }
}
