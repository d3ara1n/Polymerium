# Plan: About Modal

Replace the current "About" menu item's `PopMessage` notification with a proper modal dialog.

## Current State

- `MainWindowContext.About()` shows a simple growl notification with version & date
- The macOS app menu "About Polymerium" triggers this

## Goal

- Create an `AboutModal` (or `AboutDialog`) that shows:
  - App icon
  - App name (`Program.Brand`)
  - Version (`Program.Version`)
  - Release date (`Program.ReleaseDate`)
  - Links (GitHub repo, etc.)
  - License / credits (optional)
- Follow the project's existing Modal/Dialog pattern (see `src/Polymerium.Avalonia/Modals/` and `src/Polymerium.Avalonia/Dialogs/`)
- Conform to the Huskui UI framework's dialog conventions

## Steps

1. Create `AboutModal.axaml` + `AboutModal.axaml.cs` (or dialog equivalent)
2. Wire up from `MainWindowContext.About()` — use `OverlayService.PopModal()` or equivalent
3. Update the `AboutCommand` handler from `PopMessage` to `PopModal`
4. Add localization keys if needed

## References

- Existing modals: `src/Polymerium.Avalonia/Modals/`
- Existing dialogs: `src/Polymerium.Avalonia/Dialogs/`
- Overlay service: `src/Polymerium.Avalonia/Services/OverlayService.cs`