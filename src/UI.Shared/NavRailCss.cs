namespace ClearMeasure.Bootcamp.UI.Shared;

internal static class NavRailCss
{
    public static string AppContainerClass(bool isNarrowViewport, bool navVisible) =>
        !isNarrowViewport && !navVisible ? "modern-app rail-collapsed" : "modern-app";

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
