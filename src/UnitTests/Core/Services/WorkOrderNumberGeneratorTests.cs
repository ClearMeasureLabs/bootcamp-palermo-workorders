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

        Assert.That(number, Has.Length.EqualTo(7));
    }
}