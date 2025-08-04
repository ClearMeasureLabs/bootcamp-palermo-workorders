using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;
public class WorkOrderArchiveTests : AcceptanceTestBase
{

    [Test]
    public async Task ShouldArchiveFromComplete()
    {
        await LoginAsCurrentUser();

        // Create draft
        var order = await CreateAndSaveNewWorkOrder();

        // Assign
        order = await ClickWorkOrderNumberFromSearchPage(order);
        order = await AssignExistingWorkOrder(order, CurrentUser.UserName);

        // Begin
        order = await ClickWorkOrderNumberFromSearchPage(order);
        order = await BeginExistingWorkOrder(order);

        // Complete
        order = await ClickWorkOrderNumberFromSearchPage(order);
        order = await CompleteExistingWorkOrder(order);

        // Archive
        order = await ClickWorkOrderNumberFromSearchPage(order);
        order = await ArchiveCompletedWorkOrder(order);
    }

    [Test]
    public async Task ShouldNotArchiveFromAssigned()
    {
        await LoginAsCurrentUser();

        // Create draft
        var order = await CreateAndSaveNewWorkOrder();

        // Assign
        order = await ClickWorkOrderNumberFromSearchPage(order);
        order = await AssignExistingWorkOrder(order, CurrentUser.UserName);


        order = await ClickWorkOrderNumberFromSearchPage(order);
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.CommandButton) + CompleteToArchivedCommand.Name)).ToBeHiddenAsync();

    }
}
