namespace ClearMeasure.Bootcamp.Core.Model;

/// <summary>
/// Named priority levels for a <see cref="WorkOrder"/>.
/// </summary>
public static class WorkOrderPriority
{
    public const string Low = "Low";
    public const string Normal = "Normal";
    public const string High = "High";

    /// <summary>
    /// Returns the canonical priority name for a case-insensitive input, or null when invalid.
    /// </summary>
    public static string? TryParse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (string.Equals(value.Trim(), Low, StringComparison.OrdinalIgnoreCase))
        {
            return Low;
        }

        if (string.Equals(value.Trim(), Normal, StringComparison.OrdinalIgnoreCase))
        {
            return Normal;
        }

        if (string.Equals(value.Trim(), High, StringComparison.OrdinalIgnoreCase))
        {
            return High;
        }

        return null;
    }
}
