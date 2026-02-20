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
        var workOrder = new WorkOrder { Title = "old title", Description = "old description" };
        var responseText = "TITLE: New Title\nDESCRIPTION: New description with proper grammar.";

        var result = WorkOrderReformatAgent.ParseResponse(responseText, workOrder);

        result.ShouldNotBeNull();
        result.Title.ShouldBe("New Title");
        result.Description.ShouldBe("New description with proper grammar.");
    }

    [Test]
    public void ShouldReturnNullWhenNoChangesNeeded()
    {
        var workOrder = new WorkOrder { Title = "Same title", Description = "Same description" };
        var responseText = "TITLE: Same title\nDESCRIPTION: Same description";

        var result = WorkOrderReformatAgent.ParseResponse(responseText, workOrder);

        result.ShouldBeNull();
    }

    [Test]
    public void ShouldParseResponseWithOnlyTitleChange()
    {
        var workOrder = new WorkOrder { Title = "lowercase title", Description = "Good description." };
        var responseText = "TITLE: Lowercase title\nDESCRIPTION: Good description.";

        var result = WorkOrderReformatAgent.ParseResponse(responseText, workOrder);

        result.ShouldNotBeNull();
        result.Title.ShouldBe("Lowercase title");
        result.Description.ShouldBe("Good description.");
    }

    [Test]
    public void ShouldParseResponseWithOnlyDescriptionChange()
    {
        var workOrder = new WorkOrder { Title = "Good Title", Description = "bad grammar here" };
        var responseText = "TITLE: Good Title\nDESCRIPTION: Bad grammar here.";

        var result = WorkOrderReformatAgent.ParseResponse(responseText, workOrder);

        result.ShouldNotBeNull();
        result.Title.ShouldBe("Good Title");
        result.Description.ShouldBe("Bad grammar here.");
    }

    [Test]
    public void ShouldDefaultToOriginalTitleWhenMissingFromResponse()
    {
        var workOrder = new WorkOrder { Title = "Original Title", Description = "old desc" };
        var responseText = "DESCRIPTION: New corrected description.";

        var result = WorkOrderReformatAgent.ParseResponse(responseText, workOrder);

        result.ShouldNotBeNull();
        result.Title.ShouldBe("Original Title");
        result.Description.ShouldBe("New corrected description.");
    }

    [Test]
    public void ShouldDefaultToOriginalDescriptionWhenMissingFromResponse()
    {
        var workOrder = new WorkOrder { Title = "old title", Description = "Original Description" };
        var responseText = "TITLE: Old title";

        var result = WorkOrderReformatAgent.ParseResponse(responseText, workOrder);

        result.ShouldNotBeNull();
        result.Title.ShouldBe("Old title");
        result.Description.ShouldBe("Original Description");
    }

    [Test]
    public void ShouldHandleCaseInsensitivePrefixes()
    {
        var workOrder = new WorkOrder { Title = "test", Description = "test" };
        var responseText = "title: Corrected Title\ndescription: Corrected description.";

        var result = WorkOrderReformatAgent.ParseResponse(responseText, workOrder);

        result.ShouldNotBeNull();
        result.Title.ShouldBe("Corrected Title");
        result.Description.ShouldBe("Corrected description.");
    }

    [Test]
    public void ShouldReturnNullWhenResponseHasNoParsableContent()
    {
        var workOrder = new WorkOrder { Title = "My Title", Description = "My Description" };
        var responseText = "Some unexpected LLM response without proper format";

        var result = WorkOrderReformatAgent.ParseResponse(responseText, workOrder);

        result.ShouldBeNull();
    }

    [Test]
    public void ShouldTrimWhitespaceFromParsedValues()
    {
        var workOrder = new WorkOrder { Title = "old", Description = "old" };
        var responseText = "TITLE:   New Title With Spaces   \nDESCRIPTION:   Cleaned up description.   ";

        var result = WorkOrderReformatAgent.ParseResponse(responseText, workOrder);

        result.ShouldNotBeNull();
        result.Title.ShouldBe("New Title With Spaces");
        result.Description.ShouldBe("Cleaned up description.");
    }
}
