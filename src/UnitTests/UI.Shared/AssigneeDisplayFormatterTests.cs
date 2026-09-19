using ClearMeasure.Bootcamp.UI.Shared;
using NUnit.Framework;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared;

public class AssigneeDisplayFormatterTests
{
    [Test]
    public void Null_ShouldReturnUnassigned()
    {
        AssigneeDisplayFormatter.Format(null).ShouldBe("Unassigned");
    }

    [Test]
    public void EmptyString_ShouldReturnUnassigned()
    {
        AssigneeDisplayFormatter.Format("").ShouldBe("Unassigned");
    }

    [Test]
    public void Whitespace_ShouldReturnUnassigned()
    {
        AssigneeDisplayFormatter.Format("  ").ShouldBe("Unassigned");
    }

    [Test]
    public void ValidName_ShouldReturnNameUnchanged()
    {
        AssigneeDisplayFormatter.Format("Homer Simpson").ShouldBe("Homer Simpson");
    }
}
