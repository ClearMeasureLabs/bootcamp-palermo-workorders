using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

[TestFixture]
public class EnvironmentsDocPresenceTests
{
    [Test]
    public void EnvironmentsDoc_WhenRead_ExistsInDocsFolder()
    {
        var path = FindRepoFile(Path.Combine("docs", "environments.md"));
        File.Exists(path).ShouldBeTrue();
    }

    [Test]
    public void EnvironmentsDoc_WhenRead_ContainsTddEnvironment()
    {
        var path = FindRepoFile(Path.Combine("docs", "environments.md"));
        File.ReadAllText(path).ShouldContain("TDD");
    }

    [Test]
    public void EnvironmentsDoc_WhenRead_ContainsUatEnvironment()
    {
        var path = FindRepoFile(Path.Combine("docs", "environments.md"));
        File.ReadAllText(path).ShouldContain("UAT");
    }

    [Test]
    public void EnvironmentsDoc_WhenRead_ContainsProductionEnvironment()
    {
        var path = FindRepoFile(Path.Combine("docs", "environments.md"));
        File.ReadAllText(path).ShouldContain("Production");
    }

    [Test]
    public void EnvironmentsDoc_WhenRead_HasH1Heading()
    {
        var path = FindRepoFile(Path.Combine("docs", "environments.md"));
        File.ReadAllText(path).ShouldStartWith("# ");
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
}
