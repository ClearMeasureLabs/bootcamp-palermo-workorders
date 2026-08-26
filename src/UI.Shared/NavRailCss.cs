namespace ClearMeasure.Bootcamp.UI.Shared;

/// <summary>
/// Pure CSS class helpers for the MainLayout left navigation rail toggle
/// (wide collapse vs narrow overlay). Keep in sync with <c>MainLayout.razor.css</c>.
/// </summary>
internal static class NavRailCss
{
    /// <summary>
    /// App shell class: wide + hidden adds <c>rail-collapsed</c> so content goes full width.
    /// </summary>
    public static string AppContainerClass(bool isNarrowViewport, bool navVisible) =>
        !isNarrowViewport && !navVisible ? "modern-app rail-collapsed" : "modern-app";

    /// <summary>
    /// Sidebar class: narrow + visible → <c>open</c> overlay; wide + hidden → <c>rail-hidden</c>.
    /// </summary>
    public static string SidebarClass(bool isNarrowViewport, bool navVisible)
    {
        const string baseClass = "modern-sidebar";
        if (isNarrowViewport)
        {
            return navVisible ? $"{baseClass} open" : baseClass;
        }

        return navVisible ? baseClass : $"{baseClass} rail-hidden";
    }
}
