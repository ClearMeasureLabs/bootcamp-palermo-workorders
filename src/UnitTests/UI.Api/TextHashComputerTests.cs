using ClearMeasure.Bootcamp.UI.Api;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class TextHashComputerTests
{
    [Test]
    public void ShouldReturnKnownDigests_When_HelloInput()
    {
        var (sha256, md5, sha1) = TextHashComputer.Compute("hello");

        sha256.ShouldBe("2cf24dba5fb0a30e26e83b2ac5b9e29e1b161e5c1fa7425e73043362938b9824");
        md5.ShouldBe("5d41402abc4b2a76b9719d911017c592");
        sha1.ShouldBe("aaf4c61ddcc5e8a2dabede0f3b482cd9aea9434d");
    }

    [Test]
    public void ShouldHashUtf8Bytes_When_NonAsciiInput()
    {
        var (sha256, _, _) = TextHashComputer.Compute("héllo");

        sha256.ShouldBe("3c48591d8d098a4538f5e013dfcf406e948eac4d3277b10bf614e295d6068179");
    }
}
