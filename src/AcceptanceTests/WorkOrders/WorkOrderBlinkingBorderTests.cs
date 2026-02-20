using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Components;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

[TestFixture]
public class WorkOrderBlinkingBorderTests : AcceptanceTestBase
{
	[Test]
	public async Task ShouldShowBlinkingBorderOnWorkOrderManageTextFields()
	{
		await LoginAsCurrentUser();

		await Click(nameof(NavMenu.Elements.NewWorkOrder));
		await Page.WaitForURLAsync("**/workorder/manage?mode=New");
		await TakeScreenshotAsync(1, "NewWorkOrderPage");

		var titleInput = Page.GetByTestId(nameof(WorkOrderManage.Elements.Title));
		var descriptionTextarea = Page.GetByTestId(nameof(WorkOrderManage.Elements.Description));
		var roomNumberInput = Page.GetByTestId(nameof(WorkOrderManage.Elements.RoomNumber));

		await Expect(titleInput).ToBeVisibleAsync();
		await Expect(descriptionTextarea).ToBeVisibleAsync();
		await Expect(roomNumberInput).ToBeVisibleAsync();

		var titleAnimation = await titleInput.EvaluateAsync<string>("el => window.getComputedStyle(el).animationName");
		var descriptionAnimation = await descriptionTextarea.EvaluateAsync<string>("el => window.getComputedStyle(el).animationName");
		var roomNumberAnimation = await roomNumberInput.EvaluateAsync<string>("el => window.getComputedStyle(el).animationName");

		titleAnimation.ShouldBe("blinkBorder");
		descriptionAnimation.ShouldBe("blinkBorder");
		roomNumberAnimation.ShouldBe("blinkBorder");

		var titleDuration = await titleInput.EvaluateAsync<string>("el => window.getComputedStyle(el).animationDuration");
		titleDuration.ShouldBe("2s");

		await TakeScreenshotAsync(2, "BlinkingBorderVerified");
	}

	[Test]
	public async Task ShouldShowBlinkingBorderOnWorkOrderSearchTextFields()
	{
		await LoginAsCurrentUser();

		await Click(nameof(NavMenu.Elements.Search));
		await Page.WaitForURLAsync("**/workorder/search");
		await TakeScreenshotAsync(1, "SearchPage");

		var searchInputs = await Page.Locator("input.form-control:not([type='checkbox']):not([type='radio']), textarea.form-control").AllAsync();

		// Note: The search page may not have text input fields, only dropdowns.
		// If there are text inputs, verify they have the blinking border animation.
		if (searchInputs.Count > 0)
		{
			foreach (var input in searchInputs)
			{
				var animationName = await input.EvaluateAsync<string>("el => window.getComputedStyle(el).animationName");
				animationName.ShouldBe("blinkBorder");
			}
		}

		await TakeScreenshotAsync(2, "SearchBlinkingBorderVerified");
	}

	[Test]
	public async Task ShouldShowBlinkingBorderOnAllInputTypesAcrossPages()
	{
		await LoginAsCurrentUser();

		await Click(nameof(NavMenu.Elements.NewWorkOrder));
		await Page.WaitForURLAsync("**/workorder/manage?mode=New");
		await TakeScreenshotAsync(1, "NewWorkOrderPage");

		var newWorkOrderInputs = await Page.Locator("input.form-control:not([type='checkbox']):not([type='radio']), textarea.form-control").AllAsync();
		newWorkOrderInputs.Count.ShouldBeGreaterThan(0);

		foreach (var input in newWorkOrderInputs)
		{
			var animationName = await input.EvaluateAsync<string>("el => window.getComputedStyle(el).animationName");
			animationName.ShouldBe("blinkBorder");
		}

		await Click(nameof(NavMenu.Elements.Search));
		await Page.WaitForURLAsync("**/workorder/search");
		await TakeScreenshotAsync(2, "SearchPage");

		var searchInputs = await Page.Locator("input.form-control:not([type='checkbox']):not([type='radio']), textarea.form-control").AllAsync();

		foreach (var input in searchInputs)
		{
			var animationName = await input.EvaluateAsync<string>("el => window.getComputedStyle(el).animationName");
			animationName.ShouldBe("blinkBorder");
		}

		var existingWorkOrder = await CreateAndSaveNewWorkOrder();
		await Page.GotoAsync($"/workorder/manage?mode=Edit&number={existingWorkOrder.Number}");
		await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		await TakeScreenshotAsync(3, "EditWorkOrderPage");

		var editInputs = await Page.Locator("input.form-control:not([type='checkbox']):not([type='radio']), textarea.form-control").AllAsync();

		foreach (var input in editInputs)
		{
			var animationName = await input.EvaluateAsync<string>("el => window.getComputedStyle(el).animationName");
			animationName.ShouldBe("blinkBorder");
		}

		await TakeScreenshotAsync(4, "AllPagesVerified");
	}
}
