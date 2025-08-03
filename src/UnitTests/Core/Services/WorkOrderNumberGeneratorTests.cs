using ClearMeasure.Bootcamp.Core.Services.Impl;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Services;

[TestFixture]
public class WorkOrderNumberGeneratorTests
{
    [Test]
    public void ShouldBeFiveInLength()
    {
        var generator = new WorkOrderNumberGenerator();
        var number = generator.GenerateNumber();

        Assert.That(number.Length, Is.EqualTo(5));
    }
}