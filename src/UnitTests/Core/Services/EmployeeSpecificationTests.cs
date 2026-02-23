using ClearMeasure.Bootcamp.Core.Services;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Services;

[TestFixture]
public class EmployeeSpecificationTests
{
    [Test]
    public void ShouldDefaultCanFulfillToFalse()
    {
        var spec = new EmployeeSpecification();
        Assert.That(spec.CanFulfill, Is.False);
    }

    [Test]
    public void ShouldSetCanFulfillViaConstructor()
    {
        var spec = new EmployeeSpecification(true);
        Assert.That(spec.CanFulfill, Is.True);
    }

    [Test]
    public void ShouldSetCanFulfillViaProperty()
    {
        var spec = new EmployeeSpecification();
        spec.CanFulfill = true;
        Assert.That(spec.CanFulfill, Is.True);
    }

    [Test]
    public void AllShouldReturnDefaultSpecification()
    {
        var spec = EmployeeSpecification.All;
        Assert.That(spec, Is.Not.Null);
        Assert.That(spec.CanFulfill, Is.False);
    }
}
