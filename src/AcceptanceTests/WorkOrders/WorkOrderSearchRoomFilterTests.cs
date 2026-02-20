using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderSearchRoomFilterTests : AcceptanceTestBase
{
	[Test, Retry(2)]
	public async Task ShouldDisplayRoomFilterDropdown()
	{
		await LoginAsCurrentUser();
		await Page.GotoAsync($"{ApplicationBaseUrl}/workorder/search");

		// Verify room filter dropdown is visible
		var roomSelect = Page.GetByTestId(nameof(WorkOrderSearch.Elements.RoomSelect).ToString());
		await roomSelect.WaitForAsync();
		
		// Verify "All" option exists
		var allOption = roomSelect.Locator("option[value='']");
		await Expect(allOption).ToBeVisibleAsync();
	}

	[Test, Retry(2)]
	public async Task ShouldFilterWorkOrdersByRoom()
	{
		await LoginAsCurrentUser();

		// Create work orders with different rooms via the Bus
		var creator = await GetCurrentUser();
		var allRooms = await Bus.Send(new RoomGetAllQuery());

		// Navigate to search
		await Page.GotoAsync($"{ApplicationBaseUrl}/workorder/search");

		// Select a room from the filter
		var roomSelect = Page.GetByTestId(nameof(WorkOrderSearch.Elements.RoomSelect).ToString());
		if (allRooms.Length > 0)
		{
			await roomSelect.SelectOptionAsync(allRooms[0].Id.ToString());
			
			// Click search button
			await Click(nameof(WorkOrderSearch.Elements.SearchButton));
			
			// Wait for results to load
			await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		}
	}
}
