using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

[TestFixture]
public class CoreCoberturaPresenceTests
{
    [Test]
    public void HasProductionCoreHits_WhenCoreFileHasHits_ReturnsTrue()
    {
        CoreCoberturaPresence.HasProductionCoreHits(CoreWithHitsFixture).ShouldBeTrue();
    }

    [Test]
    public void HasProductionCoreHits_WhenOnlyUnitTestsCorePaths_ReturnsFalse()
    {
        CoreCoberturaPresence.HasProductionCoreHits(UnitTestsCoreOnlyFixture).ShouldBeFalse();
    }

    [Test]
    public void HasProductionCoreHits_WhenCorePresentButZeroHits_ReturnsFalse()
    {
        CoreCoberturaPresence.HasProductionCoreHits(CoreZeroHitsFixture).ShouldBeFalse();
    }

    [Test]
    public void IsProductionCoreFilename_WhenWindowsSrcCorePath_ReturnsTrue()
    {
        CoreCoberturaPresence.IsProductionCoreFilename(
                @"D:\repo\src\Core\Model\Employee.cs")
            .ShouldBeTrue();
    }

    [Test]
    public void IsProductionCoreFilename_WhenUnitTestsCorePath_ReturnsFalse()
    {
        CoreCoberturaPresence.IsProductionCoreFilename(
                @"D:\repo\src\UnitTests\Core\Model\EmployeeTests.cs")
            .ShouldBeFalse();
    }

    [Test]
    public void CoverletRunSettings_WhenRead_IncludesBootcampAndExcludesTests()
    {
        var path = FindRepoFile("coverlet.runsettings");
        var xml = File.ReadAllText(path);

        xml.ShouldContain("<Include>[ClearMeasure.Bootcamp.*]*</Include>");
        xml.ShouldContain("UnitTests");
        xml.ShouldContain("IntegrationTests");
        xml.ShouldContain("AcceptanceTests");
        xml.ShouldContain("cobertura");
    }

    [Test]
    public void BuildScript_WhenRead_PassesCoverletRunSettingsToUnitAndIntegration()
    {
        var path = FindRepoFile("build.ps1");
        var source = File.ReadAllText(path);

        source.ShouldContain("coverlet.runsettings");
        source.ShouldContain("--settings:$coverletRunSettings");
    }

    [Test]
    public void AssertCoreCoberturaScript_WhenRead_FailsWhenCoreMissing()
    {
        var path = FindRepoFile(Path.Combine(
            ".cursor", "skills", "crap-score-cleanup", "scripts", "assert-core-cobertura.ps1"));
        var source = File.ReadAllText(path);

        source.ShouldContain("Test-HasProductionCoreHits");
        source.ShouldContain("src/core");
        source.ShouldContain("exit 1");
    }

    private static string FindRepoFile(string relativePath)
    {
        var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, relativePath);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        throw new FileNotFoundException($"Repo file not found: {relativePath}");
    }

    private const string CoreWithHitsFixture = """
        <?xml version="1.0" encoding="utf-8"?>
        <coverage>
          <packages>
            <package name="ClearMeasure.Bootcamp.Core">
              <classes>
                <class name="ClearMeasure.Bootcamp.Core.Model.Employee" filename="src/Core/Model/Employee.cs" line-rate="1">
                  <lines>
                    <line number="10" hits="3" />
                  </lines>
                </class>
              </classes>
            </package>
          </packages>
        </coverage>
        """;

    private const string UnitTestsCoreOnlyFixture = """
        <?xml version="1.0" encoding="utf-8"?>
        <coverage>
          <packages>
            <package name="ClearMeasure.Bootcamp.UnitTests">
              <classes>
                <class name="ClearMeasure.Bootcamp.UnitTests.Core.Model.EmployeeTests" filename="src/UnitTests/Core/Model/EmployeeTests.cs" line-rate="1">
                  <lines>
                    <line number="10" hits="5" />
                  </lines>
                </class>
              </classes>
            </package>
          </packages>
        </coverage>
        """;

    private const string CoreZeroHitsFixture = """
        <?xml version="1.0" encoding="utf-8"?>
        <coverage>
          <packages>
            <package name="ClearMeasure.Bootcamp.Core">
              <classes>
                <class name="ClearMeasure.Bootcamp.Core.Model.Employee" filename="src/Core/Model/Employee.cs" line-rate="0">
                  <lines>
                    <line number="10" hits="0" />
                  </lines>
                </class>
              </classes>
            </package>
          </packages>
        </coverage>
        """;
}
