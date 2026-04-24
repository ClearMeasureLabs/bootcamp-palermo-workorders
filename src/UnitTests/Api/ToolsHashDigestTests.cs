using ClearMeasure.Bootcamp.UI.Api;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Api;

[TestFixture]
public class ToolsHashDigestTests
{
    [Test]
    public void Should_ReturnExpectedSha256_When_TextIsAbc()
    {
        var result = ToolsHashDigest.Compute("abc", includeLegacyHashes: false);

        result.Sha256.ShouldBe("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad");
        result.Md5.ShouldBeNull();
        result.Sha1.ShouldBeNull();
    }

    [Test]
    public void Should_ReturnLowercaseHex_When_LegacyRequested()
    {
        var result = ToolsHashDigest.Compute("abc", includeLegacyHashes: true);

        result.Md5.ShouldBe("900150983cd24fb0d6963f7d28e17f72");
        result.Sha1.ShouldBe("a9993e364706816aba3e25717850c26c9cd0d89d");
    }

    [Test]
    public void Should_ReturnWellDefinedDigests_When_TextIsEmpty()
    {
        var result = ToolsHashDigest.Compute("", includeLegacyHashes: true);

        result.Sha256.ShouldBe("e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855");
        result.Md5.ShouldBe("d41d8cd98f00b204e9800998ecf8427e");
        result.Sha1.ShouldBe("da39a3ee5e6b4b0d3255bfef95601890afd80709");
    }
}
