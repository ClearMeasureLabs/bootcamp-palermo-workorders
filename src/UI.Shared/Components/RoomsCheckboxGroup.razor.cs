using Microsoft.AspNetCore.Components;

namespace ClearMeasure.Bootcamp.UI.Shared.Components;

public partial class RoomsCheckboxGroup
{
    [Parameter]
    public List<string> SelectedRooms { get; set; } = new();

    [Parameter]
    public EventCallback<List<string>> SelectedRoomsChanged { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    private readonly List<string> AvailableRooms = new()
    {
        "Choir",
        "Kitchen",
        "Chapel",
        "Nursery",
        "Foyer"
    };

    private async Task HandleCheckboxChange(string room, ChangeEventArgs e)
    {
        var isChecked = (bool)(e.Value ?? false);
        
        if (isChecked && !SelectedRooms.Contains(room))
        {
            SelectedRooms.Add(room);
        }
        else if (!isChecked && SelectedRooms.Contains(room))
        {
            SelectedRooms.Remove(room);
        }

        await SelectedRoomsChanged.InvokeAsync(SelectedRooms);
    }
}
