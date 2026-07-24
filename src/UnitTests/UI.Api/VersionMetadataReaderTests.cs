using ClearMeasure.Bootcamp.UI.Api;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class VersionMetadataReaderTests
{
    [Test]
    public void Should_Parse_CommitHash_From_InformationalVersion_With_PlusSuffix()
    {
        var buildVersion = VersionMetadataReader.ReadBuildVersion(typeof(VersionMetadataReader).Assembly);
        var commitHash = VersionMetadataReader.ReadCommitHash("1.4.123+abc1234def5678");

        buildVersion.ShouldNotBeNullOrEmpty();
        commitHash.ShouldBe("abc1234def5678");
    }

    [Test]
    public void Should_Return_NullCommitHash_When_InformationalVersion_Has_No_PlusSuffix()
    {
        var commitHash = VersionMetadataReader.ReadCommitHash("1.0.0");

        commitHash.ShouldBeNull();
    }

    [Test]
    public void Should_Return_NullCommitHash_When_InformationalVersion_Is_NullOrEmpty()
    {
        VersionMetadataReader.ReadCommitHash(null).ShouldBeNull();
        VersionMetadataReader.ReadCommitHash("").ShouldBeNull();
    }

    [Test]
    public void Should_Return_NullCommitHash_When_InformationalVersion_Ends_With_Plus()
    {
        VersionMetadataReader.ReadCommitHash("1.0.0+").ShouldBeNull();
    }
}
