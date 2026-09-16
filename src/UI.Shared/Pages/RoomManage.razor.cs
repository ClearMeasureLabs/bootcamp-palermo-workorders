using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared.Models;
using Microsoft.AspNetCore.Components;

namespace ClearMeasure.Bootcamp.UI.Shared.Pages;

[Route("/rooms")]
[Route("/rooms/manage")]
public partial class RoomManage : AppComponentBase
{
    private Room[] _rooms = [];
    private bool _showForm;
    private RoomManageModel _formModel = new();
    private Guid? _confirmDeleteId;

    protected override async Task OnInitializedAsync()
    {
        await LoadRooms();
    }

    private async Task LoadRooms()
    {
        _rooms = await Bus.Send(new RoomGetAllQuery());
    }

    private void ShowNewRoomForm()
    {
        _formModel = new RoomManageModel();
        _showForm = true;
        _confirmDeleteId = null;
    }

    private void ShowEditRoomForm(Room room)
    {
        _formModel = new RoomManageModel { Id = room.Id, Name = room.Name };
        _showForm = true;
        _confirmDeleteId = null;
    }

    private void CancelForm()
    {
        _showForm = false;
        _formModel = new RoomManageModel();
    }

    private async Task HandleSubmit()
    {
        var room = new Room { Id = _formModel.Id, Name = _formModel.Name! };
        await Bus.Send(new SaveRoomCommand(room));
        _showForm = false;
        _formModel = new RoomManageModel();
        await LoadRooms();
    }

    private void ShowDeleteConfirmation(Guid roomId)
    {
        _confirmDeleteId = roomId;
        _showForm = false;
    }

    private async Task ConfirmDelete()
    {
        if (_confirmDeleteId.HasValue)
        {
            await Bus.Send(new DeleteRoomCommand(_confirmDeleteId.Value));
            _confirmDeleteId = null;
            await LoadRooms();
        }
    }

    private void CancelDelete()
    {
        _confirmDeleteId = null;
    }
}
