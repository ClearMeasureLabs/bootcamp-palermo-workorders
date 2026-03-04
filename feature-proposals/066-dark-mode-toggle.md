## Why
Many users prefer dark mode for reduced eye strain, especially during extended use or in low-light environments. Supporting dark mode improves accessibility and aligns with modern application expectations.

## What Changes
- Add `ThemeToggle.razor` component in `src/UI.Shared/Components/` with a toggle button switching between light and dark modes
- Add `ThemeService` in `src/UI/Client/` that reads/writes the theme preference to `localStorage` and applies the corresponding CSS class to the document root
- Add `dark-theme.css` in `src/UI/Client/wwwroot/css/` defining CSS custom properties (variables) for dark mode colors (background, text, borders, cards, inputs, status badges)
- Modify `MainLayout.razor` in `src/UI/Client/` to include the `ThemeToggle` component in the header area
- Modify `app.css` in `src/UI/Client/wwwroot/css/` to use CSS custom properties instead of hardcoded colors for all themeable elements
- Add JavaScript interop function in `src/UI/Client/wwwroot/js/` to detect `prefers-color-scheme: dark` system preference and apply on initial load before Blazor hydrates
- Update NavMenu, WorkOrderSearch, and WorkOrderManage components to use CSS variable-based theming

## Capabilities
### New Capabilities
- Dark mode theme with adjusted background, text, border, and component colors
- Toggle button in the application header to switch between light and dark modes
- Theme preference persisted in `localStorage` across sessions
- System preference detection on first visit (respects `prefers-color-scheme: dark`)
- Smooth CSS transition when switching themes

### Modified Capabilities
- All existing pages and components updated to use CSS custom properties for colors

## Impact
- **UI.Shared**: New `ThemeToggle.razor` component
- **UI.Client**: New `ThemeService`, new `dark-theme.css`, modified `app.css` to use CSS variables, modified `MainLayout.razor`, new JS interop file
- **Database**: No schema changes required
- **Dependencies**: No new NuGet packages required

## Acceptance Criteria
### Unit Tests
- `ThemeToggle_WhenClicked_TogglesThemeClass` - bUnit test confirming clicking the toggle switches the CSS class
- `ThemeToggle_RendersCorrectIcon_ForLightMode` - bUnit test confirming sun/moon icon matches current mode
- `ThemeToggle_RendersCorrectIcon_ForDarkMode` - bUnit test confirming icon state in dark mode

### Integration Tests
- None (client-side only feature with no server/database interaction)

### Acceptance Tests
- Click the theme toggle button with `data-testid="theme-toggle"` and verify the page body has the `dark-theme` CSS class
- Click the toggle again and verify the page returns to light mode without the `dark-theme` class
- Set dark mode, refresh the page, and verify dark mode persists via `localStorage`
- Verify all key pages (WorkOrderSearch, WorkOrderManage, NavMenu) render correctly in dark mode without text or contrast issues
