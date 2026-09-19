namespace ClearMeasure.Bootcamp.UI.Shared;

/// <summary>
/// Display formatting for room numbers in the work order search table.
/// </summary>
public static class RoomNumberDisplayFormatter
{
    /// <summary>
    /// Returns the room number trimmed, or an em dash when the input is null, empty, or whitespace.
    /// </summary>
    public static string Format(string? roomNumber)
    {
        if (string.IsNullOrWhiteSpace(roomNumber))
            return "\u2014";

        return roomNumber.Trim();
    }
}
