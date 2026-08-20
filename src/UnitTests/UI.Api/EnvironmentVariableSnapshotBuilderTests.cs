using ClearMeasure.Bootcamp.UI.Api;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class EnvironmentVariableSnapshotBuilderTests
{
    private const string UnlistedVariableName = "EnvironmentVariableSnapshotBuilderTests_UnlistedKey";

    [TearDown]
    public void TearDown()
    {
        Environment.SetEnvironmentVariable(UnlistedVariableName, null);
        Environment.SetEnvironmentVariable("DATABASE_ENGINE", null);
    }

    [Test]
    public void Build_Should_ReturnRedactedEntries_ForAllowlist()
    {
        Environment.SetEnvironmentVariable("DATABASE_ENGINE", "SQL-Container");

        var entries = EnvironmentVariableSnapshotBuilder.Build();

        var match = entries.SingleOrDefault(e => e.Name == "DATABASE_ENGINE");
        match.ShouldNotBeNull();
        match!.Value.ShouldBe(EnvironmentVariableSnapshotBuilder.RedactedValue);
    }

    [Test]
    public void Build_Should_NotIncludeNonAllowlistedVariables()
    {
        Environment.SetEnvironmentVariable(UnlistedVariableName, "secret-value");

        var entries = EnvironmentVariableSnapshotBuilder.Build();

        entries.Any(e => e.Name == UnlistedVariableName).ShouldBeFalse();
        entries.All(e => e.Value == EnvironmentVariableSnapshotBuilder.RedactedValue).ShouldBeTrue();
    }
}
