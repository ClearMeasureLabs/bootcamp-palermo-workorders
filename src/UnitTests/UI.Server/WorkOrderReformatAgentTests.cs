using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.UI.Server;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class WorkOrderReformatAgentTests
{
    [Test]
    public void ShouldParseResponseWithTitleAndDescription()
    {
        var workOrder = new WorkOrder { Title = "old title", Description = "OLD DESCRIPTION" };
        var responseText = "TITLE: New Title\nDESCRIPTION: NEW DESCRIPTION WITH PROPER GRAMMAR.";

        var result = WorkOrderReformatAgent.ParseResponse(responseText, workOrder);

        result.ShouldNotBeNull();
        result.Title.ShouldBe("New Title");
        result.Description.ShouldBe("NEW DESCRIPTION WITH PROPER GRAMMAR.");
    }

    [Test]
    public void ShouldReturnNullWhenNoChangesNeeded()
    {
        var workOrder = new WorkOrder { Title = "Same title", Description = "SAME DESCRIPTION" };
        var responseText = "TITLE: Same title\nDESCRIPTION: SAME DESCRIPTION";

        var result = WorkOrderReformatAgent.ParseResponse(responseText, workOrder);

        result.ShouldBeNull();
    }

    [Test]
    public void ShouldParseResponseWithOnlyTitleChange()
    {
        var workOrder = new WorkOrder { Title = "lowercase title", Description = "GOOD DESCRIPTION." };
        var responseText = "TITLE: Lowercase title\nDESCRIPTION: GOOD DESCRIPTION.";

        var result = WorkOrderReformatAgent.ParseResponse(responseText, workOrder);

        result.ShouldNotBeNull();
        result.Title.ShouldBe("Lowercase title");
        result.Description.ShouldBe("GOOD DESCRIPTION.");
    }

    [Test]
    public void ShouldParseResponseWithOnlyDescriptionChange()
    {
        var workOrder = new WorkOrder { Title = "Good Title", Description = "BAD GRAMMAR HERE" };
        var responseText = "TITLE: Good Title\nDESCRIPTION: BAD GRAMMAR HERE.";

        var result = WorkOrderReformatAgent.ParseResponse(responseText, workOrder);

        result.ShouldNotBeNull();
        result.Title.ShouldBe("Good Title");
        result.Description.ShouldBe("BAD GRAMMAR HERE.");
    }

    [Test]
    public void ShouldDefaultToOriginalTitleWhenMissingFromResponse()
    {
        var workOrder = new WorkOrder { Title = "Original Title", Description = "OLD DESC" };
        var responseText = "DESCRIPTION: NEW CORRECTED DESCRIPTION.";

        var result = WorkOrderReformatAgent.ParseResponse(responseText, workOrder);

        result.ShouldNotBeNull();
        result.Title.ShouldBe("Original Title");
        result.Description.ShouldBe("NEW CORRECTED DESCRIPTION.");
    }

    [Test]
    public void ShouldDefaultToOriginalDescriptionWhenMissingFromResponse()
    {
        var workOrder = new WorkOrder { Title = "old title", Description = "ORIGINAL DESCRIPTION" };
        var responseText = "TITLE: Old title";

        var result = WorkOrderReformatAgent.ParseResponse(responseText, workOrder);

        result.ShouldNotBeNull();
        result.Title.ShouldBe("Old title");
        result.Description.ShouldBe("ORIGINAL DESCRIPTION");
    }

    [Test]
    public void ShouldHandleCaseInsensitivePrefixes()
    {
        var workOrder = new WorkOrder { Title = "test", Description = "TEST" };
        var responseText = "title: Corrected Title\ndescription: CORRECTED DESCRIPTION.";

        var result = WorkOrderReformatAgent.ParseResponse(responseText, workOrder);

        result.ShouldNotBeNull();
        result.Title.ShouldBe("Corrected Title");
        result.Description.ShouldBe("CORRECTED DESCRIPTION.");
    }

    [Test]
    public void ShouldReturnNullWhenResponseHasNoParsableContent()
    {
        var workOrder = new WorkOrder { Title = "My Title", Description = "MY DESCRIPTION" };
        var responseText = "Some unexpected LLM response without proper format";

        var result = WorkOrderReformatAgent.ParseResponse(responseText, workOrder);

        result.ShouldBeNull();
    }

    [Test]
    public void ShouldTrimWhitespaceFromParsedValues()
    {
        var workOrder = new WorkOrder { Title = "old", Description = "OLD" };
        var responseText = "TITLE:   New Title With Spaces   \nDESCRIPTION:   CLEANED UP DESCRIPTION.   ";

        var result = WorkOrderReformatAgent.ParseResponse(responseText, workOrder);

        result.ShouldNotBeNull();
        result.Title.ShouldBe("New Title With Spaces");
        result.Description.ShouldBe("CLEANED UP DESCRIPTION.");
    }
}
