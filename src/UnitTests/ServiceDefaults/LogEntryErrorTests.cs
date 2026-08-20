using Microsoft.Extensions.Hosting;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.ServiceDefaults;

[TestFixture]
public class LogEntryErrorTests
{
    [Test]
    public void Constructor_ShouldLeavePropertiesNull_WhenExceptionIsNull()
    {
        var error = new LogEntryError(null);

        error.Type.ShouldBeNull();
        error.Message.ShouldBeNull();
        error.StackTrace.ShouldBeNull();
        error.InnerException.ShouldBeNull();
    }

    [Test]
    public void Constructor_ShouldMapExceptionFields_WhenExceptionProvided()
    {
        Exception thrown;
        try
        {
            throw new InvalidOperationException("outer");
        }
        catch (Exception ex)
        {
            thrown = ex;
        }

        var error = new LogEntryError(thrown);

        error.Type.ShouldBe(typeof(InvalidOperationException).FullName);
        error.Message.ShouldBe("outer");
        error.StackTrace.ShouldNotBeNull();
        error.InnerException.ShouldBeNull();
    }

    [Test]
    public void Constructor_ShouldMapInnerException_WhenPresent()
    {
        var inner = new ArgumentException("inner");
        var outer = new InvalidOperationException("outer", inner);

        var error = new LogEntryError(outer);

        error.InnerException.ShouldNotBeNull();
        error.InnerException!.Message.ShouldBe("inner");
        error.InnerException.Type.ShouldBe(typeof(ArgumentException).FullName);
    }

    [Test]
    public void ParameterlessConstructor_ShouldAllowPropertyAssignment()
    {
        var error = new LogEntryError
        {
            Type = "T",
            Message = "M"
        };

        error.Type.ShouldBe("T");
        error.Message.ShouldBe("M");
    }
}
