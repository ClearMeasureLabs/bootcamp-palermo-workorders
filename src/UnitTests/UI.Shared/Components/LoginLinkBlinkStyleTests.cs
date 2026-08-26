using System.Text.RegularExpressions;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Components;

[TestFixture]
public class LoginLinkBlinkStyleTests
{
    [Test]
    public void ShouldPulseOpacityBetweenFullAndPartial_SoTheBlinkIsVisible()
    {
        var css = ReadLoginLinkCss();

        var trough = Regex.Match(css, @"50%\s*\{[^}]*opacity:\s*(?<opacity>[\d.]+)");
        trough.Success.ShouldBeTrue("Expected a 50% keyframe declaring opacity");

        var opacity = double.Parse(trough.Groups["opacity"].Value);
        opacity.ShouldBeGreaterThanOrEqualTo(0.5);
        opacity.ShouldBeLessThanOrEqualTo(0.6);
    }

    [Test]
    public void ShouldNeverFullyHideTheLoginLabel()
    {
        var css = ReadLoginLinkCss();

        foreach (Match match in Regex.Matches(css, @"opacity:\s*(?<opacity>[\d.]+)"))
        {
            double.Parse(match.Groups["opacity"].Value).ShouldBeGreaterThanOrEqualTo(0.55);
        }
    }

    [Test]
    public void ShouldUseGentleRepeatingCycle()
    {
        var css = ReadLoginLinkCss();

        css.ShouldContain("animation: login-link-blink-pulse 1.5s ease-in-out infinite");
    }

    [Test]
    public void ShouldOutrankMainLayoutAuthSectionAnchorRule()
    {
        var css = ReadLoginLinkCss();

        css.ShouldContain(".auth-section a.login-link-blink");
    }

    [Test]
    public void ShouldNotUseGlobalPseudoClass_BecauseBrowsersDropThoseRules()
    {
        var css = ReadLoginLinkCss();

        css.ShouldNotContain(":global(");
    }

    [Test]
    public void ShouldAccentBothThemes()
    {
        var css = ReadLoginLinkCss();

        css.ShouldContain("#2b6cb0");
        css.ShouldContain("#90cdf4");
        css.ShouldContain("html[data-theme=\"dark\"] .auth-section a.login-link-blink");
    }

    [Test]
    public void ShouldDisableAnimation_WhenReducedMotionRequested()
    {
        var css = ReadLoginLinkCss();

        var reducedMotion = Regex.Match(
            css,
            @"@media \(prefers-reduced-motion: reduce\) \{(?<body>.*)\}\s*$",
            RegexOptions.Singleline);
        reducedMotion.Success.ShouldBeTrue("Expected a prefers-reduced-motion block");
        reducedMotion.Groups["body"].Value.ShouldContain("animation: none");
    }

    private static string ReadLoginLinkCss()
    {
        var path = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "UI.Shared", "Components", "LoginLink.razor.css"));

        File.Exists(path).ShouldBeTrue($"Expected scoped stylesheet at {path}");
        var css = File.ReadAllText(path).Replace("\r\n", "\n");
        return Regex.Replace(css, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline).Trim();
    }
}
