using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;
internal class WorkOrderShelveTests : AcceptanceTestBase
{
    [Test]
    public async Task ShouldCompleteWorkOrder()
    {
        await LoginAsCurrentUser();

        var order = await CreateAndSaveNewWorkOrder();
        order = await ClickWorkOrderNumberFromSearchPage(order);
        order = await AssignExistingWorkOrder(order, CurrentUser.UserName);
        order = await ClickWorkOrderNumberFromSearchPage(order);

        order = await BeginExistingWorkOrder(order);
        order = await ClickWorkOrderNumberFromSearchPage(order);

        order.Title = "Title";
        order.Description = "Description";
        order = await ShelveExistingWorkOrder(order);
        order = await ClickWorkOrderNumberFromSearchPage(order);

        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Title))).ToHaveValueAsync(order.Title!,
            new LocatorAssertionsToHaveValueOptions
            {
                Timeout = 10000 // 10 seconds
            });

        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Title))).ToBeDisabledAsync();
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Description)))
            .ToHaveValueAsync(order.Description!);
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Description))).ToBeDisabledAsync();
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Status)))
            .ToHaveTextAsync(WorkOrderStatus.Assigned.FriendlyName);
    }
}
