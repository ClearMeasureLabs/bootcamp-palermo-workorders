using System.Diagnostics;
using System.Globalization;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using System.Text.RegularExpressions;
using ClearMeasure.Bootcamp.Core.Queries;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderChatTests : AcceptanceTestBase
{
    [Test]
    public async Task ShouldAppearOnWorkOrderScreens()
    {
        await LoginAsCurrentUser();

        WorkOrder order = await CreateAndSaveNewWorkOrder();

        await ClickWorkOrderNumberFromSearchPage(order);
        order = await AssignExistingWorkOrder(order, CurrentUser.UserName);
        order = await ClickWorkOrderNumberFromSearchPage(order);

        await Expect(Page.GetByTestId("work-order-chat")).ToBeVisibleAsync();

        Console.WriteLine(order);
    }

    [Ignore("India's computer didn't want to download the Llama")]
    [Test]
    public async Task ShouldReturnAnyInformationThatIsNotAnError()
    {
        await LoginAsCurrentUser();

        WorkOrder order = await CreateAndSaveNewWorkOrder();

        await ClickWorkOrderNumberFromSearchPage(order);
        order = await AssignExistingWorkOrder(order, CurrentUser.UserName);
        order = await ClickWorkOrderNumberFromSearchPage(order);

        await Input("work-order-chat", "What is the status of this item?");

        await Expect(Page.GetByTestId("work-order-chat")).Not
            .ToHaveTextAsync("Error");

        Console.WriteLine(order);
    }
}