using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.Rooms;

public class RoomManageTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task ShouldCreateEditAndDeleteRoom_WhenLeaderUsesRoomsPage()
    {
        await LoginAsCurrentUser();

        await Click(nameof(NavMenu.Elements.Rooms));
        await Page.WaitForURLAsync("**/rooms");

        var nameA = $"[{TestTag}] Room A";
        await Click(nameof(RoomManage.Elements.NewRoomButton));
        await Input(nameof(RoomManage.Elements.NameInput), nameA);
        await Click(nameof(RoomManage.Elements.SaveButton));

        var roomNameA = Page.GetByTestId(nameof(RoomManage.Elements.RoomName)).Filter(new LocatorFilterOptions { HasText = nameA });
        await Expect(roomNameA).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 30_000 });

        var rowA = Page.GetByTestId(nameof(RoomManage.Elements.RoomRow)).Filter(new LocatorFilterOptions { HasText = nameA });
        await rowA.GetByTestId(nameof(RoomManage.Elements.EditButton)).ClickAsync();

        var nameB = $"[{TestTag}] Room B";
        await Input(nameof(RoomManage.Elements.NameInput), nameB);
        await Click(nameof(RoomManage.Elements.SaveButton));

        var roomNameB = Page.GetByTestId(nameof(RoomManage.Elements.RoomName)).Filter(new LocatorFilterOptions { HasText = nameB });
        await Expect(roomNameB).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 30_000 });
        (await Page.GetByTestId(nameof(RoomManage.Elements.RoomName)).Filter(new LocatorFilterOptions { HasText = nameA }).CountAsync()).ShouldBe(0);

        var rowB = Page.GetByTestId(nameof(RoomManage.Elements.RoomRow)).Filter(new LocatorFilterOptions { HasText = nameB });
        await rowB.GetByTestId(nameof(RoomManage.Elements.DeleteButton)).ClickAsync();

        var deleteConfirmation = Page.GetByTestId(nameof(RoomManage.Elements.DeleteConfirmation));
        await Expect(deleteConfirmation).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 30_000 });
        await Click(nameof(RoomManage.Elements.ConfirmDeleteButton));

        await Expect(rowB).ToHaveCountAsync(0, new LocatorAssertionsToHaveCountOptions { Timeout = 30_000 });

        var rooms = await Bus.Send(new RoomGetAllQuery());
        rooms.ShouldNotContain(room => room.Name == nameA);
        rooms.ShouldNotContain(room => room.Name == nameB);
    }
}
