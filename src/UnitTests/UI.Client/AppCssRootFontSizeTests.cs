using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Client;

[TestFixture]
public class AppCssRootFontSizeTests
{
    private static string ReadAppCss()
    {
        var path = LocateAppCss();
        path.ShouldNotBeNull($"Could not locate src/UI/Client/wwwroot/css/app.css from {AppContext.BaseDirectory}");
        return File.ReadAllText(path);
    }

    private static string? LocateAppCss()
    {
        var relative = Path.Combine("src", "UI", "Client", "wwwroot", "css", "app.css");
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, relative);
            if (File.Exists(candidate))
                return candidate;
            dir = dir.Parent;
        }

        return null;
    }

    [Test]
    public void Should_SetDesktopRootFontSize_To15px()
    {
        var css = ReadAppCss();

        css.ShouldContain("font-size: 15px");
        css.ShouldContain("scroll-behavior: smooth");
    }

    [Test]
    public void Should_SetMobileRootFontSize_To13px()
    {
        var css = ReadAppCss();
        var mediaIndex = css.IndexOf("@media (max-width: 768px)", StringComparison.Ordinal);
        mediaIndex.ShouldBeGreaterThanOrEqualTo(0);

        var mediaBlock = css[mediaIndex..];
        var nextRuleIndex = mediaBlock.IndexOf("/* Root typography", StringComparison.Ordinal);
        if (nextRuleIndex < 0)
            nextRuleIndex = mediaBlock.IndexOf("/* Smooth scrolling", StringComparison.Ordinal);
        if (nextRuleIndex > 0)
            mediaBlock = mediaBlock[..nextRuleIndex];

        mediaBlock.ShouldContain("font-size: 13px");
        mediaBlock.ShouldNotContain("font-size: 14px");
    }

    [Test]
    public void Should_KeepScrollBehaviorSmooth_OnHtml()
    {
        var css = ReadAppCss();
        css.ShouldContain("scroll-behavior: smooth");
    }
}
