using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Queries;
using Microsoft.AspNetCore.Components;

namespace ClearMeasure.Bootcamp.UI.Shared.Components;

public partial class RoomsCheckboxGroup
{
    [Inject]
    private IBus? Bus { get; set; }

    [Parameter]
    public List<string> SelectedRooms { get; set; } = new();

    [Parameter]
    public EventCallback<List<string>> SelectedRoomsChanged { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    private List<string> AvailableRooms { get; set; } = new();

    protected override async Task OnInitializedAsync()
    {
        if (Bus != null)
        {
            var rooms = await Bus.Send(new RoomGetAllQuery());
            AvailableRooms = rooms.Select(r => r.Name).ToList();
        }
    }

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
