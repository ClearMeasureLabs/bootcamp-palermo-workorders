namespace ClearMeasure.Bootcamp.Core.Model;

/// <summary>
/// Defines the recurrence pattern for recurring work orders.
/// </summary>
public enum RecurrencePattern
{
    /// <summary>
    /// One-time work order (no recurrence).
    /// </summary>
    None = 0,
    
    /// <summary>
    /// Repeats every N weeks.
    /// </summary>
    Weekly = 1,
    
    /// <summary>
    /// Repeats every N months.
    /// </summary>
    Monthly = 2,
    
    /// <summary>
    /// Repeats every N quarters (3 months).
    /// </summary>
    Quarterly = 3,
    
    /// <summary>
    /// Repeats every N years.
    /// </summary>
    Annually = 4
}
