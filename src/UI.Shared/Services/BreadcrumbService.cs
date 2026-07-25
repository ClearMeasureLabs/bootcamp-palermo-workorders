using ClearMeasure.Bootcamp.UI.Shared.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace ClearMeasure.Bootcamp.UI.Shared.Services;

/// <summary>
/// Builds the breadcrumb trail from the current route.
/// </summary>
public sealed class BreadcrumbService : IDisposable
{
    private readonly NavigationManager _navigation;
    private IReadOnlyList<BreadcrumbItem> _items = Array.Empty<BreadcrumbItem>();
    private bool _shouldShow;

    public BreadcrumbService(NavigationManager navigation)
    {
        _navigation = navigation;
        _navigation.LocationChanged += OnLocationChanged;
        UpdateFromCurrentLocation();
    }

    public event Action? OnChange;

    public IReadOnlyList<BreadcrumbItem> Items => _items;

    public bool ShouldShow => _shouldShow;

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e) => UpdateFromCurrentLocation();

    internal void UpdateFromCurrentLocation()
    {
        var uri = _navigation.ToAbsoluteUri(_navigation.Uri);
        var path = uri.AbsolutePath;

        if (string.IsNullOrEmpty(path))
            path = "/";

        if (path.Length > 1 && path.EndsWith('/'))
            path = path.TrimEnd('/');

        var (shouldShow, items) = BuildTrail(path, uri.Query);
        _shouldShow = shouldShow;
        _items = items;
        OnChange?.Invoke();
    }

    private static (bool ShouldShow, IReadOnlyList<BreadcrumbItem> Items) BuildTrail(string path, string query)
    {
        if (path == "/" ||
            path.Equals("/login", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/_clienthealthcheck", StringComparison.OrdinalIgnoreCase))
        {
            return (false, Array.Empty<BreadcrumbItem>());
        }

        if (path.Equals("/counter", StringComparison.OrdinalIgnoreCase))
            return (true, Trail(Home(), Active("Counter")));

        if (path.Equals("/fetchdata", StringComparison.OrdinalIgnoreCase))
            return (true, Trail(Home(), Active("Fetch Data")));

        if (path.Equals("/ai-agent", StringComparison.OrdinalIgnoreCase))
            return (true, Trail(Home(), Active("AI Agent")));

        if (path.Equals("/settings", StringComparison.OrdinalIgnoreCase))
            return (true, Trail(Home(), Active("Settings")));

        if (path.Equals("/workorder/search", StringComparison.OrdinalIgnoreCase))
            return (true, Trail(Home(), WorkOrders(), Active("Search")));

        if (path.Equals("/workorder/manage", StringComparison.OrdinalIgnoreCase))
            return (true, Trail(Home(), WorkOrders(), Active("New Work Order")));

        if (path.StartsWith("/workorder/manage/", StringComparison.OrdinalIgnoreCase))
        {
            var id = path["/workorder/manage/".Length..];
            if (!string.IsNullOrWhiteSpace(id))
                return (true, Trail(Home(), WorkOrders(), Active(id)));
        }

        return (false, Array.Empty<BreadcrumbItem>());
    }

    private static BreadcrumbItem Home() => new("Home", "/", false);

    private static BreadcrumbItem WorkOrders() => new("Work Orders", "/workorder/search", false);

    private static BreadcrumbItem Active(string label) => new(label, null, true);

    private static IReadOnlyList<BreadcrumbItem> Trail(params BreadcrumbItem[] items) => items;

    public void Dispose() => _navigation.LocationChanged -= OnLocationChanged;
}
