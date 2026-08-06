namespace ClearMeasure.Bootcamp.UnitTests.UI.Client;

/// <summary>
/// Source-guards for the global root typography step-down in app.css.
/// bUnit does not load wwwroot CSS, so these assert the Technical Design values in source.
/// </summary>
[TestFixture]
public class AppCssRootFontSizeTests
{
    private static string ReadAppCss()
    {
        var appCss = FindAppCssPath();
        appCss.ShouldNotBeNull($"Could not locate src/UI/Client/wwwroot/css/app.css from {AppContext.BaseDirectory}");
        return File.ReadAllText(appCss);
    }

    private static string? FindAppCssPath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "src", "UI", "Client", "wwwroot", "css", "app.css");
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
        var nextRule = mediaBlock.IndexOf("/* Smooth scrolling", StringComparison.Ordinal);
        if (nextRule > 0)
            mediaBlock = mediaBlock[..nextRule];

        mediaBlock.ShouldContain("font-size: 13px");
        mediaBlock.ShouldNotContain("font-size: 14px");
    }
}
