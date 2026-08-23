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
        return [Draft, Assigned, InProgress, Complete, Cancelled];
    }

    public string Code { get; }

    public string Key { get; }

    public string FriendlyName { get; set; }

    public byte SortBy { get; set; }

    /// <inheritdoc />
    public bool Equals(WorkOrderStatus? other) =>
        ReferenceEquals(this, other) || HasSameCode(other);

    private bool HasSameCode(WorkOrderStatus? other) =>
        other is not null
        && GetType() == other.GetType()
        && CodesEqual(other.Code);

    private bool CodesEqual(string? otherCode) =>
        // Uninitialized (null!) Code never matches by value — including vs another null Code.
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        Code is not null && string.Equals(Code, otherCode, StringComparison.Ordinal);

    public override bool Equals(object? obj) => Equals(obj as WorkOrderStatus);

    public override string ToString()
    {
        return FriendlyName;
    }

    public override int GetHashCode()
    {
        // Null-Code instances are never equal by value (see Equals); a fixed sentinel
        // avoids NullReferenceException during materialization. Parameterless ctor uses
        // null! so NRT claims Code is never null — suppress that contract mismatch.
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        return Code is null ? 0 : Code.GetHashCode();
    }

    /// <summary>
    /// Value equality by <see cref="Code"/> (not reference). Thin wrapper over
    /// <see cref="Equals(WorkOrderStatus?)"/> so Qodana reference-comparison findings
    /// and CRAP stay on the covered Equals path.
    /// </summary>
    public static bool operator ==(WorkOrderStatus? left, WorkOrderStatus? right) =>
        left?.Equals(right) ?? right is null;

    public static bool operator !=(WorkOrderStatus? left, WorkOrderStatus? right) =>
        !(left == right);

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