using ClearMeasure.Bootcamp.Core.Exceptions;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Services;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Model.StateCommands;

[TestFixture]
public class ValidationTests
{
    [Test]
    public void ShouldThrowValidationExceptionWhenTitleIsNull()
    {
        var order = new WorkOrder
        {
            Status = WorkOrderStatus.Draft,
            Title = null,
            Description = "Valid description"
        };
        var employee = new Employee();
        order.Creator = employee;

        var command = new SaveDraftCommand(order, employee);

        var ex = Assert.Throws<ValidationException>(() => 
            command.Execute(new StateCommandContext()));
        
        ex.Errors.ContainsKey("Title").ShouldBeTrue();
    }

    [Test]
    public void ShouldThrowValidationExceptionWhenTitleIsEmpty()
    {
        var order = new WorkOrder
        {
            Status = WorkOrderStatus.Draft,
            Title = "",
            Description = "Valid description"
        };
        var employee = new Employee();
        order.Creator = employee;

        var command = new SaveDraftCommand(order, employee);

        var ex = Assert.Throws<ValidationException>(() => 
            command.Execute(new StateCommandContext()));
        
        ex.Errors.ContainsKey("Title").ShouldBeTrue();
    }

    [Test]
    public void ShouldThrowValidationExceptionWhenTitleIsWhitespace()
    {
        var order = new WorkOrder
        {
            Status = WorkOrderStatus.Draft,
            Title = "   ",
            Description = "Valid description"
        };
        var employee = new Employee();
        order.Creator = employee;

        var command = new SaveDraftCommand(order, employee);

        var ex = Assert.Throws<ValidationException>(() => 
            command.Execute(new StateCommandContext()));
        
        ex.Errors.ContainsKey("Title").ShouldBeTrue();
    }

    [Test]
    public void ShouldThrowValidationExceptionWhenDescriptionIsNull()
    {
        var order = new WorkOrder
        {
            Status = WorkOrderStatus.Draft,
            Title = "Valid title",
            Description = null
        };
        var employee = new Employee();
        order.Creator = employee;

        var command = new SaveDraftCommand(order, employee);

        var ex = Assert.Throws<ValidationException>(() => 
            command.Execute(new StateCommandContext()));
        
        ex.Errors.ContainsKey("Description").ShouldBeTrue();
    }

    [Test]
    public void ShouldThrowValidationExceptionWhenDescriptionIsEmpty()
    {
        var order = new WorkOrder
        {
            Status = WorkOrderStatus.Draft,
            Title = "Valid title",
            Description = ""
        };
        var employee = new Employee();
        order.Creator = employee;

        var command = new SaveDraftCommand(order, employee);

        var ex = Assert.Throws<ValidationException>(() => 
            command.Execute(new StateCommandContext()));
        
        ex.Errors.ContainsKey("Description").ShouldBeTrue();
    }

    [Test]
    public void ShouldThrowValidationExceptionWhenBothTitleAndDescriptionAreInvalid()
    {
        var order = new WorkOrder
        {
            Status = WorkOrderStatus.Draft,
            Title = "",
            Description = ""
        };
        var employee = new Employee();
        order.Creator = employee;

        var command = new SaveDraftCommand(order, employee);

        var ex = Assert.Throws<ValidationException>(() => 
            command.Execute(new StateCommandContext()));
        
        ex.Errors.ContainsKey("Title").ShouldBeTrue();
        ex.Errors.ContainsKey("Description").ShouldBeTrue();
        ex.Errors.Count.ShouldBe(2);
    }

    [Test]
    public void ShouldNotThrowValidationExceptionWhenBothFieldsAreValid()
    {
        var order = new WorkOrder
        {
            Status = WorkOrderStatus.Draft,
            Title = "Valid title",
            Description = "Valid description"
        };
        var employee = new Employee();
        order.Creator = employee;

        var command = new SaveDraftCommand(order, employee);

        Assert.DoesNotThrow(() => command.Execute(new StateCommandContext()));
    }
}
