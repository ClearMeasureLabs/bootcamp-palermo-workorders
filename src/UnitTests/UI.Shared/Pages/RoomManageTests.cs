using Bunit;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Palermo.BlazorMvc;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;

[TestFixture]
public class RoomManageTests
{
    private static StubRoomBus BuildBus(Room[]? rooms = null) => new(rooms ?? []);

    private static BunitContext BuildContext(StubRoomBus bus)
    {
        var ctx = new BunitContext();
        ctx.Services.AddSingleton<IBus>(bus);
        ctx.Services.AddSingleton<IUiBus>(new StubUiBus());
        return ctx;
    }

    [Test]
    public async Task ShouldRenderRoomList_WhenPageLoads()
    {
        var rooms = new[]
        {
            new Room { Id = Guid.NewGuid(), Name = "Boardroom" },
            new Room { Id = Guid.NewGuid(), Name = "Kitchen" }
        };
        var bus = BuildBus(rooms);
        await using var ctx = BuildContext(bus);

        var component = ctx.Render<RoomManage>();

        await component.WaitForAssertionAsync(() =>
        {
            var rows = component.FindAll($"[data-testid='{RoomManage.Elements.RoomName}']");
            rows.Count.ShouldBe(2);
            rows[0].TextContent.ShouldContain("Boardroom");
            rows[1].TextContent.ShouldContain("Kitchen");
        });
    }

    [Test]
    public async Task ShouldShowNewRoomForm_WhenNewRoomButtonClicked()
    {
        var bus = BuildBus();
        await using var ctx = BuildContext(bus);

        var component = ctx.Render<RoomManage>();

        await component.Find($"[data-testid='{RoomManage.Elements.NewRoomButton}']").ClickAsync(new());

        await component.WaitForAssertionAsync(() =>
        {
            component.Find($"[data-testid='{RoomManage.Elements.NameInput}']").ShouldNotBeNull();
        });
    }

    [Test]
    public async Task ShouldSendSaveRoomCommand_WhenFormSubmitted()
    {
        var bus = BuildBus();
        await using var ctx = BuildContext(bus);

        var component = ctx.Render<RoomManage>();

        await component.Find($"[data-testid='{RoomManage.Elements.NewRoomButton}']").ClickAsync(new());
        await component.Find($"[data-testid='{RoomManage.Elements.NameInput}']").ChangeAsync(new() { Value = "New Room A" });
        await component.Find($"[data-testid='{RoomManage.Elements.SaveButton}']").ClickAsync(new());

        await component.WaitForAssertionAsync(() =>
        {
            bus.LastSaveCommand.ShouldNotBeNull();
            bus.LastSaveCommand!.Room.Name.ShouldBe("New Room A");
        });
    }

    [Test]
    public async Task ShouldSendSaveRoomCommand_WhenEditFormSubmitted()
    {
        var existingId = Guid.NewGuid();
        var rooms = new[] { new Room { Id = existingId, Name = "Old Name" } };
        var bus = BuildBus(rooms);
        await using var ctx = BuildContext(bus);

        var component = ctx.Render<RoomManage>();

        await component.WaitForAssertionAsync(() =>
            component.FindAll($"[data-testid='{RoomManage.Elements.EditButton}']").Count.ShouldBe(1));

        await component.Find($"[data-testid='{RoomManage.Elements.EditButton}']").ClickAsync(new());
        await component.Find($"[data-testid='{RoomManage.Elements.NameInput}']").ChangeAsync(new() { Value = "Updated Name" });
        await component.Find($"[data-testid='{RoomManage.Elements.SaveButton}']").ClickAsync(new());

        await component.WaitForAssertionAsync(() =>
        {
            bus.LastSaveCommand.ShouldNotBeNull();
            bus.LastSaveCommand!.Room.Name.ShouldBe("Updated Name");
            bus.LastSaveCommand.Room.Id.ShouldBe(existingId);
        });
    }

    [Test]
    public async Task ShouldSendDeleteRoomCommand_WhenDeleteConfirmed()
    {
        var roomId = Guid.NewGuid();
        var rooms = new[] { new Room { Id = roomId, Name = "Room to Delete" } };
        var bus = BuildBus(rooms);
        await using var ctx = BuildContext(bus);

        var component = ctx.Render<RoomManage>();

        await component.WaitForAssertionAsync(() =>
            component.FindAll($"[data-testid='{RoomManage.Elements.DeleteButton}']").Count.ShouldBe(1));

        await component.Find($"[data-testid='{RoomManage.Elements.DeleteButton}']").ClickAsync(new());

        await component.WaitForAssertionAsync(() =>
            component.Find($"[data-testid='{RoomManage.Elements.ConfirmDeleteButton}']").ShouldNotBeNull());

        await component.Find($"[data-testid='{RoomManage.Elements.ConfirmDeleteButton}']").ClickAsync(new());

        await component.WaitForAssertionAsync(() =>
        {
            bus.LastDeleteCommand.ShouldNotBeNull();
            bus.LastDeleteCommand!.RoomId.ShouldBe(roomId);
        });
    }

    [Test]
    public async Task ShouldNotSendDeleteRoomCommand_WhenDeleteCancelled()
    {
        var roomId = Guid.NewGuid();
        var rooms = new[] { new Room { Id = roomId, Name = "Room to Keep" } };
        var bus = BuildBus(rooms);
        await using var ctx = BuildContext(bus);

        var component = ctx.Render<RoomManage>();

        await component.WaitForAssertionAsync(() =>
            component.FindAll($"[data-testid='{RoomManage.Elements.DeleteButton}']").Count.ShouldBe(1));

        await component.Find($"[data-testid='{RoomManage.Elements.DeleteButton}']").ClickAsync(new());

        await component.WaitForAssertionAsync(() =>
            component.Find($"[data-testid='{RoomManage.Elements.CancelDeleteButton}']").ShouldNotBeNull());

        await component.Find($"[data-testid='{RoomManage.Elements.CancelDeleteButton}']").ClickAsync(new());

        await component.WaitForAssertionAsync(() =>
        {
            bus.LastDeleteCommand.ShouldBeNull();
        });
    }

    private sealed class StubRoomBus : Bus
    {
        private Room[] _rooms;

        // ReSharper disable once ConvertToPrimaryConstructor -- epic guardrail: no mass primary-constructor conversion
        public StubRoomBus(Room[] rooms) : base(null!)
        {
            _rooms = rooms;
        }

        public SaveRoomCommand? LastSaveCommand { get; private set; }
        public DeleteRoomCommand? LastDeleteCommand { get; private set; }

        public override Task Publish(INotification notification) => Task.CompletedTask;

        public override Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
        {
            if (request is RoomGetAllQuery)
            {
                return Task.FromResult((TResponse)(object)_rooms);
            }

            if (request is SaveRoomCommand save)
            {
                LastSaveCommand = save;
                if (save.Room.Id == Guid.Empty)
                {
                    save.Room.Id = Guid.NewGuid();
                }
                _rooms = [.. _rooms.Where(r => r.Id != save.Room.Id), save.Room];
                return Task.FromResult((TResponse)(object)save.Room);
            }

            if (request is DeleteRoomCommand delete)
            {
                LastDeleteCommand = delete;
                _rooms = _rooms.Where(r => r.Id != delete.RoomId).ToArray();
                return Task.FromResult((TResponse)(object)Unit.Value);
            }

            throw new NotImplementedException(request.GetType().Name);
        }
    }
}
