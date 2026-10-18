namespace ClearMeasure.Bootcamp.UI.Shared.Models;

public static class EditModeExtensions
{
    public static string ToSubmitLabel(this EditMode mode)
    {
        return mode switch
        {
            EditMode.New => "Create",
            EditMode.Edit => "Save",
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };
    }
}
