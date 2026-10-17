using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Services.Impl;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Services;

[TestFixture]
public class WorkOrderNumberGeneratorTests
{
    [Test]
    public void ShouldBeSevenInLength()
    {
        var generator = new WorkOrderNumberGenerator();
        var number = generator.GenerateNumber();

        Assert.That(number.Length, Is.EqualTo(WorkOrder.NumberMaxLength));
    }

    [Test]
    public void ShouldBeUpperCase()
    {
        var generator = new WorkOrderNumberGenerator();
        var number = generator.GenerateNumber();

        Assert.That(number, Is.EqualTo(number.ToUpperInvariant()));
    }
}