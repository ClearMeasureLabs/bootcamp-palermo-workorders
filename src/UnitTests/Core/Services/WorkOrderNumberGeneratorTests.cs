using System.Globalization;
using ClearMeasure.Bootcamp.Core.Services.Impl;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Services;

[TestFixture]
public class WorkOrderNumberGeneratorTests
{
    [Test]
    public void ShouldBeSevenInLength()
    {
        var generator = new WorkOrderNumberGenerator();
        var number = generator.GenerateNumber();

        Assert.That(number.Length, Is.EqualTo(7));
    }

    [Test]
    public void GenerateNumber_WithTurkishCulture_ShouldUpperCaseInvariantly()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
            var generator = new WorkOrderNumberGenerator();

            var number = generator.GenerateNumber();

            number.ShouldBe(number.ToUpperInvariant());
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }
}