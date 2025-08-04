using System.Diagnostics;
using System.Globalization;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using System.Text.RegularExpressions;
using ClearMeasure.Bootcamp.Core.Queries;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderSaveChatTests : AcceptanceTestBase
{
    [Test]
    public async Task ShouldAppearOnWorkOrderScreens()
    {
        await LoginAsCurrentUser();

        WorkOrder order = await CreateAndSaveNewWorkOrder();

        await ClickWorkOrderNumberFromSearchPage(order);
        order = await AssignExistingWorkOrder(order, CurrentUser.UserName);
        order = await ClickWorkOrderNumberFromSearchPage(order);

        //test code india
        var test = Page.GetByTestId("work-order-chat");
        var test1 = Page.GetByText("Ask the AI assistant about this work order...");

        await Expect(Page.GetByText("Ask the AI assistant about this work order..."))
            .ToBeVisibleAsync();

        Console.WriteLine(order);
    }
}