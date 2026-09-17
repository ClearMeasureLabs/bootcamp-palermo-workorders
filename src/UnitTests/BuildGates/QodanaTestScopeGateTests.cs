using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

[TestFixture]
public class QodanaTestScopeGateTests
{
    [Test]
    public void QodanaYaml_StillHasWholeSolutionFailThresholdZero()
    {
        var yaml = File.ReadAllText(FindRepoFile("qodana.yaml"));

        yaml.ShouldContain("failThreshold: 0");
        yaml.ShouldContain("solution: src/ChurchBulletin.sln");
    }

    [Test]
    public void BuildYml_QodanaStep_StillPassesFailThresholdZero()
    {
        var workflow = File.ReadAllText(FindRepoFile(Path.Combine(".github", "workflows", "build.yml")));

        workflow.ShouldContain("--fail-threshold,0");
        workflow.ShouldContain("--solution,src/ChurchBulletin.sln");
    }

    [Test]
    public void UnitTests_EditorConfig_DowngradesRedundantUsingAndNullableSuppression()
    {
        AssertEditorConfigDowngrades(Path.Combine("src", "UnitTests", ".editorconfig"));
    }

    [Test]
    public void IntegrationTests_EditorConfig_DowngradesRedundantUsingAndNullableSuppression()
    {
        AssertEditorConfigDowngrades(Path.Combine("src", "IntegrationTests", ".editorconfig"));
    }

    [Test]
    public void AcceptanceTests_EditorConfig_DowngradesRedundantUsingAndNullableSuppression()
    {
        AssertEditorConfigDowngrades(Path.Combine("src", "AcceptanceTests", ".editorconfig"));
    }

    [Test]
    public void SrcEditorConfig_DoesNotDowngradeQodanaGatingInspections()
    {
        var config = File.ReadAllText(FindRepoFile(Path.Combine("src", ".editorconfig")));

        config.ShouldNotContain("resharper_redundant_using_directive_highlighting = none");
        config.ShouldNotContain("resharper_redundant_nullable_warning_suppression_highlighting = none");
    }

    private static void AssertEditorConfigDowngrades(string relativePath)
    {
        var config = File.ReadAllText(FindRepoFile(relativePath));

        config.ShouldContain("resharper_redundant_using_directive_highlighting = none");
        config.ShouldContain("resharper_redundant_nullable_warning_suppression_highlighting = none");
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

        throw new FileNotFoundException($"{relativePath} not found from test directory.");
    }
}
