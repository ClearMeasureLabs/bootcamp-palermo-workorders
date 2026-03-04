## Why
The fixed-width sidebar navigation consumes valuable horizontal screen space, especially on smaller laptop screens or when users need to focus on content. A collapsible sidebar gives users control over the layout, maximizing the content area when navigation is not needed.

## What Changes
- Modify `NavMenu.razor` in `src/UI/Client/Shared/` to support collapsed and expanded states
- Add `SidebarToggle.razor` component in `src/UI.Shared/Components/` rendering a toggle button (chevron icon) at the top or bottom of the sidebar
- Add `SidebarService` in `src/UI/Client/` that manages the sidebar collapsed/expanded state and persists the preference in `localStorage`
- Modify `MainLayout.razor` in `src/UI/Client/` to adjust the content area width based on sidebar state (full sidebar width vs. icon-only width)
- Add icon-only mode for sidebar items: show only the navigation icon with a tooltip for the page name when collapsed
- Add CSS transition animation for smooth collapse/expand (width transition over 200ms)
- Add CSS styles for collapsed sidebar (narrow width ~60px, icon-only display, tooltip on hover)
- Ensure the sidebar toggle button is always visible in both states
- Add keyboard shortcut Ctrl+B to toggle the sidebar (if keyboard shortcuts feature is present)

## Capabilities
### New Capabilities
- Collapsible sidebar navigation that can be toggled between full and icon-only modes
- Toggle button to collapse/expand the sidebar
- Icon-only display when collapsed with tooltip showing page name on hover
- Collapsed/expanded state persisted in `localStorage` across sessions
- Smooth CSS transition animation during collapse/expand
- Content area automatically adjusts width when sidebar state changes

### Modified Capabilities
- NavMenu supports two display modes: expanded (icon + text) and collapsed (icon only)
- MainLayout content area dynamically resizes based on sidebar state

## Impact
- **UI.Shared**: New `SidebarToggle.razor` component
- **UI.Client**: New `SidebarService`, modified `NavMenu.razor`, modified `MainLayout.razor`, new CSS transition styles
- **Database**: No schema changes required
- **Dependencies**: No new NuGet packages required

## Acceptance Criteria
### Unit Tests
- `SidebarToggle_WhenClicked_TogglesState` - bUnit test confirming clicking the toggle button switches between collapsed and expanded
- `NavMenu_WhenCollapsed_ShowsOnlyIcons` - bUnit test confirming navigation items render without text labels in collapsed state
- `NavMenu_WhenExpanded_ShowsIconsAndLabels` - bUnit test confirming navigation items render with both icons and text labels in expanded state
- `SidebarService_PersistsState_ToLocalStorage` - service writes the collapsed/expanded boolean to `localStorage`

### Integration Tests
- None (client-side UI component with no server/database interaction)

### Acceptance Tests
- Click the sidebar toggle button with `data-testid="sidebar-toggle"` and verify the sidebar collapses to icon-only mode with `data-testid="sidebar-collapsed"`
- Verify that navigation icons are still visible and hovering over them shows a tooltip with the page name
- Click the toggle button again and verify the sidebar expands to full width with `data-testid="sidebar-expanded"`
- Collapse the sidebar, refresh the page, and verify the sidebar remains collapsed (state persists)
- Verify the content area expands to fill the space when the sidebar is collapsed
