using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.WorkOrders;

public class WorkOrderInstructionsTests : AcceptanceTestBase
{
    [Test]
    public async Task ShouldCreateWorkOrderWithInstructions()
    {
        await LoginAsCurrentUser();

        // Create a new work order with instructions
        var order = await CreateAndSaveNewWorkOrder();
        order.Instructions = "Please wear safety equipment when performing this work. Follow lockout/tagout procedures.";
        
        // Navigate to the work order edit page
        order = await ClickWorkOrderNumberFromSearchPage(order);
        
        // Fill in instructions field
        await Input(nameof(WorkOrderManage.Elements.Instructions), order.Instructions);
        
        // Save the work order
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + "Save");
        
        // Navigate back to verify instructions were saved
        order = await ClickWorkOrderNumberFromSearchPage(order);
        
        // Verify instructions field contains the expected value
        await Expect(Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions))).ToHaveValueAsync(order.Instructions!);
    }

    [Test]
    public async Task ShouldCreateWorkOrderWith4000CharacterInstructions()
    {
        await LoginAsCurrentUser();

        // Create a new work order
        var order = await CreateAndSaveNewWorkOrder();
        
        // Create 4000 character instructions text
        var longInstructions = new string('A', 4000);
        order.Instructions = longInstructions;
        
        // Navigate to the work order edit page
        order = await ClickWorkOrderNumberFromSearchPage(order);
        
        // Fill in instructions field with 4000 characters
        await Input(nameof(WorkOrderManage.Elements.Instructions), order.Instructions);
        
        // Save the work order
        await Click(nameof(WorkOrderManage.Elements.CommandButton) + "Save");
        
        // Navigate back to verify instructions were saved
        order = await ClickWorkOrderNumberFromSearchPage(order);
        
        // Verify instructions field contains exactly 4000 characters
        var instructionsElement = Page.GetByTestId(nameof(WorkOrderManage.Elements.Instructions));
        var actualValue = await instructionsElement.GetAttributeAsync("value");
        Assert.That(actualValue?.Length, Is.EqualTo(4000));
        await Expect(instructionsElement).ToHaveValueAsync(longInstructions);
    }
}