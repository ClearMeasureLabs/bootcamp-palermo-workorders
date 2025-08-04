﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

[TestFixture]   
public class WorkOrderShelveTests : AcceptanceTestBase
{
    [Test]
    public async Task ShouldAssignAndCancel()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkOrder();
        order = await ClickWorkOrderNumberFromSearchPage(order);

        order = await AssignExistingWorkOrder(order, CurrentUser.UserName);
        order = await ClickWorkOrderNumberFromSearchPage(order);
        order.Title = "Title";
        order.Description = "Description";
        await Input(nameof(WorkOrderManage.Elements.Title), order.Title);
        await Input(nameof(WorkOrderManage.Elements.Description), order.Description);
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + AssignedToInProgressCommand.Name);
        order = await ClickWorkOrderNumberFromSearchPage(order);

		await Click(nameof(WorkOrderManage.Elements.CommandButton) +
                    InProgressToAssigned.Name);
        order = await ClickWorkOrderNumberFromSearchPage(order);

        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Status)))
            .ToHaveTextAsync(WorkOrderStatus.Assigned.FriendlyName);

    }
}
