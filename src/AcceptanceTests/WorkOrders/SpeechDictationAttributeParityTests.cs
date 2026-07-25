using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

[TestFixture]
public class SpeechDictationAttributeParityTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task ShouldHaveMatchingTitleAndAriaLabelOnAllSpeechDictateButtons()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkOrder();
        order = await ClickWorkOrderNumberFromSearchPage(order);

        var testIds = new[]
        {
            nameof(WorkOrderManage.Elements.SpeakTitle),
            nameof(WorkOrderManage.Elements.DictateTitle),
            nameof(WorkOrderManage.Elements.SpeakDescription),
            nameof(WorkOrderManage.Elements.DictateDescription)
        };

        foreach (var testId in testIds)
        {
            var button = Page.GetByTestId(testId);
            await Expect(button).ToBeVisibleAsync();

            var title = await button.GetAttributeAsync("title");
            var ariaLabel = await button.GetAttributeAsync("aria-label");
            title.ShouldNotBeNull();
            ariaLabel.ShouldNotBeNull();
            title.ShouldBe(ariaLabel);
        }
    }
}
