using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

[TestFixture]
public class CrapProductionScopeTests
{
    [Test]
    public void IsProductionFile_WhenCoreSource_ReturnsTrue()
    {
        CrapProductionScope.IsProductionFile(@"D:\repo\src\Core\Model\WorkOrder.cs").ShouldBeTrue();
    }

    [Test]
    public void IsProductionFile_WhenWindowsBackslashSrcPath_ReturnsTrue()
    {
        CrapProductionScope.IsProductionFile(@"D:\bootcamp-palermo-workorders\src\McpServer\Tools\WorkOrderCommandExecutor.cs")
            .ShouldBeTrue();
    }

    [Test]
    public void IsProductionFile_WhenUnitTestPath_ReturnsFalse()
    {
        CrapProductionScope.IsProductionFile("/repo/src/UnitTests/Core/WorkOrderTests.cs").ShouldBeFalse();
    }

    [Test]
    public void IsProductionFile_WhenIntegrationTestPath_ReturnsFalse()
    {
        CrapProductionScope.IsProductionFile("/repo/src/IntegrationTests/Api/PingEndpointIntegrationTests.cs")
            .ShouldBeFalse();
    }

    [Test]
    public void IsProductionFile_WhenAcceptanceTestPath_ReturnsFalse()
    {
        CrapProductionScope.IsProductionFile("/repo/src/AcceptanceTests/App/ClientHealthCheckTests.cs")
            .ShouldBeFalse();
    }

    [Test]
    public void IsProductionFile_WhenGeneratedProto_ReturnsFalse()
    {
        CrapProductionScope.IsProductionFile("/repo/src/UI/Server/Generated/Protos/Workorders.cs")
            .ShouldBeFalse();
    }

    [Test]
    public void IsProductionFile_WhenDesignerFile_ReturnsFalse()
    {
        CrapProductionScope.IsProductionFile("/repo/src/UI/Server/Form.Designer.cs").ShouldBeFalse();
    }

    [Test]
    public void IsProductionFile_WhenGeneratedCs_ReturnsFalse()
    {
        CrapProductionScope.IsProductionFile("/repo/src/UI/Server/Workorders.g.cs").ShouldBeFalse();
    }

    [Test]
    public void IsProductionFile_WhenMissingOrOutsideSrc_ReturnsFalse()
    {
        CrapProductionScope.IsProductionFile(null).ShouldBeFalse();
        CrapProductionScope.IsProductionFile("").ShouldBeFalse();
        CrapProductionScope.IsProductionFile("/repo/tools/Foo.cs").ShouldBeFalse();
    }
}
