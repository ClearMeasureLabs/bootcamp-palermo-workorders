## Why
Power users and keyboard-centric workflows benefit significantly from keyboard shortcuts. Common actions like creating a new work order, saving, and searching can be performed faster without reaching for the mouse, improving productivity for high-volume users.

## What Changes
- Add `KeyboardShortcutService` in `src/UI/Client/` that registers global keyboard event listeners via JavaScript interop and maps key combinations to actions
- Add JavaScript interop file `src/UI/Client/wwwroot/js/keyboard-shortcuts.js` handling keydown events, modifier key detection, and preventing default browser behavior for captured shortcuts
- Add `ShortcutHelpOverlay.razor` component in `src/UI.Shared/Components/` displaying a modal with all available shortcuts, triggered by pressing `?`
- Modify `MainLayout.razor` in `src/UI/Client/` to initialize the `KeyboardShortcutService` and include the `ShortcutHelpOverlay`
- Register shortcuts: Ctrl+N (navigate to new work order page), Ctrl+S (save current work order form), Ctrl+F (focus the search input on WorkOrderSearch page), Escape (close modals/overlays), ? (toggle shortcut help overlay)
- Add `ShortcutRegistration` record in `src/UI.Shared/` with properties: KeyCombination, Description, Action
- Ensure shortcuts are suppressed when focus is inside text input fields (except Ctrl+S and Escape)

## Capabilities
### New Capabilities
- Global keyboard shortcuts for common application actions
- Ctrl+N navigates to the new work order creation page
- Ctrl+S saves the current work order form (on WorkOrderManage page)
- Ctrl+F focuses the search input field (on WorkOrderSearch page)
- Escape closes any open modal or overlay
- ? key toggles a help overlay listing all available shortcuts
- Shortcuts suppressed when typing in input fields (to avoid conflicts)

### Modified Capabilities
- MainLayout initializes keyboard shortcut listener on load

## Impact
- **UI.Shared**: New `ShortcutHelpOverlay.razor` component, `ShortcutRegistration` record
- **UI.Client**: New `KeyboardShortcutService`, new JS interop file, modified `MainLayout.razor`
- **Database**: No schema changes required
- **Dependencies**: No new NuGet packages required

## Acceptance Criteria
### Unit Tests
- `KeyboardShortcutService_RegistersAllDefaultShortcuts` - service contains all expected shortcut registrations
- `ShortcutHelpOverlay_RendersAllShortcuts` - bUnit test confirming overlay lists all registered shortcuts with key combinations and descriptions
- `ShortcutHelpOverlay_WhenClosed_IsNotVisible` - bUnit test confirming overlay is hidden by default

### Integration Tests
- None (client-side only feature with no server/database interaction)

### Acceptance Tests
- Press `?` key and verify the shortcut help overlay appears with `data-testid="shortcut-help-overlay"`
- Press `Escape` and verify the shortcut help overlay closes
- Press `Ctrl+N` and verify navigation to the new work order page
- Navigate to WorkOrderSearch, press `Ctrl+F`, and verify the search input with `data-testid="search-input"` receives focus
- Navigate to WorkOrderManage with a draft work order, modify a field, press `Ctrl+S`, and verify the form saves (confirmation indicator appears)
