using System.Text.RegularExpressions;
using ClearMeasure.Bootcamp.UI.Api;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class TextHasherTests
{
    [Test]
    public void Should_ReturnKnownSha256Md5AndSha1_When_TextIsHello()
    {
        var result = TextHasher.ComputeHashes("hello");

        result.Sha256.ShouldBe("2cf24dba5cf692ac421b552c308d25d9f161afcc3388f3c1fa4febf5bcbadbdb");
        result.Md5.ShouldBe("5d41402abc4badb7605b357e99571da9");
        result.Sha1.ShouldBe("aaf4c61ddcc5e8a2dabede0f4b3ac12fa6cebc15");
    }

    [Test]
    public void Should_ReturnDistinctDigests_When_TextDiffers()
    {
        var hello = TextHasher.ComputeHashes("hello");
        var world = TextHasher.ComputeHashes("world");

        hello.Sha256.ShouldNotBe(world.Sha256);
    }

    [Test]
    public void Should_HashUtf8Bytes_When_TextContainsNonAscii()
    {
        var result = TextHasher.ComputeHashes("héllo 🌍");

        result.Sha256.ShouldBe("cbbcee01a3fc5f1c0db23e02be25316adf28ede876031fdbabe5f4fabe47ed7f");
        result.Md5.ShouldBe("a4115cc10566f0181d01df50100b37ff");
        result.Sha1.ShouldBe("3d100d877e936e4baff8c55a424233d2383c315f");
    }

    [Test]
    public void Should_ReturnLowercaseHex_When_DigestsComputed()
    {
        var result = TextHasher.ComputeHashes("hello");

        Regex.IsMatch(result.Md5, "^[0-9a-f]{32}$").ShouldBeTrue();
        Regex.IsMatch(result.Sha1, "^[0-9a-f]{40}$").ShouldBeTrue();
        Regex.IsMatch(result.Sha256, "^[0-9a-f]{64}$").ShouldBeTrue();
    }
}
