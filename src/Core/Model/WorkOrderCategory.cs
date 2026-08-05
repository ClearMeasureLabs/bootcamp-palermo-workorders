namespace ClearMeasure.Bootcamp.Core.Model;

/// <summary>
/// Defines the category/type of work order for organizational purposes.
/// </summary>
public enum WorkOrderCategory
{
    /// <summary>
    /// Building maintenance, plumbing, electrical, structural repairs.
    /// </summary>
    Facilities = 0,
    
    /// <summary>
    /// Sound systems, projectors, lighting, streaming equipment.
    /// </summary>
    AudioVisual = 1,
    
    /// <summary>
    /// Landscaping, parking lots, outdoor areas, signage.
    /// </summary>
    Grounds = 2,
    
    /// <summary>
    /// Heating, ventilation, air conditioning systems.
    /// </summary>
    HVAC = 3,
    
    /// <summary>
    /// Miscellaneous or uncategorized work orders.
    /// </summary>
    Other = 4
}
