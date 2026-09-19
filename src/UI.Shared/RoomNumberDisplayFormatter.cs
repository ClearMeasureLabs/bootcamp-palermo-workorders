namespace ClearMeasure.Bootcamp.UI.Shared;

/// <summary>
/// Display formatting for the room number column in the work order search table.
/// Returns an em dash (U+2014) when the room number is null, empty, or whitespace.
/// </summary>
public static class RoomNumberDisplayFormatter
{
    /// <summary>
    /// Returns the em dash character (U+2014) for null/empty/whitespace input,
    /// otherwise returns the input room number unchanged.
    /// </summary>
    public static string Format(string? room)
    {
        if (string.IsNullOrWhiteSpace(room))
            return "\u2014";

        return room;
    }
}
