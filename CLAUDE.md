# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

InkOverlay is a small Windows screen-annotation tool (inspired by Epic Pen) built with C#, WPF and .NET 10 (`net10.0-windows`). It uses both WPF and WinForms (`UseWindowsForms` is on for `Screen` and `NotifyIcon`). User-facing text (UI, tooltips, tray menu, README) is in Norwegian; code and comments are in English.

## Commands

```powershell
dotnet build
dotnet run
.\build-release.ps1 -Version 1.0.1 [-Publish]   # Velopack Setup.exe into Releases\, optionally to GitHub Releases
```

Releases need the `vpk` global tool (keep its version equal to the `Velopack` package) and, for `-Publish`, `gh auth login` or `$env:GITHUB_TOKEN`.

There are no tests and no lint configuration. Only one instance can run at a time (named mutex `InkOverlay.SingleInstance`), so close a running copy (tray icon → Avslutt) before `dotnet run`, or the new process exits immediately. A running copy also locks `bin/` and makes the build fail.

## Architecture

- **Entry point is `Program.Main`, not `App.xaml`** (`App.xaml` is a `Page` in the csproj). `VelopackApp.Build()...Run()` must run before anything else: Velopack launches the exe with special arguments on install/uninstall, runs the fast-callback hooks (enable/disable `Autostart`) and exits. It also applies an update downloaded in a previous session. Installed copies live in `%LocalAppData%\InkOverlay\current\`, a path that stays stable across updates.
- **Updates (`Updater`)** come from GitHub Releases of `Busemann-ai/InkOverlay` (public, no token). At startup a background check downloads silently; the tray's "Se etter oppdateringer" applies and restarts. `ApplyUpdatesAndRestart` exits without `OnExit`, so settings are saved first. Everything Velopack-related is a no-op when `Updater.IsInstalled` is false (e.g. `dotnet run`), and the autostart/update tray items are hidden then.
- **Icon**: `Assets\InkOverlay.ico` is optional. When it exists it becomes the `ApplicationIcon`, an embedded resource (`InkOverlay.ico`, used by the tray) and the Setup icon; otherwise `SystemIcons.Application` is used.
- **`App` is the composition root and the single command surface.** Toolbar buttons, tray menu, global hotkeys and local keys all call the same `App` methods (`ToggleDraw`, `SetTool`, `Undo`, `Clear`, `CycleFade`, ...). Local keyboard shortcuts live in `App.HandleKey`, which both overlay and toolbar forward `PreviewKeyDown` to. When you add a command, wire it in every surface it belongs to and update the README's hotkey table.
- **`AppState` is the one source of truth** (tool, color, size, ink visibility, fade seconds). It raises a single `Changed` event, and each window re-applies its whole state in an `Apply()` method instead of reacting to individual properties.
- **One `OverlayWindow` per monitor**, created at startup from `Forms.Screen.AllScreens` (primary first; `_overlays[0]` is the primary one and owns the toolbar). The app is per-monitor-V2 DPI aware (`app.manifest`), so screen bounds are physical pixels and windows are placed with `SetWindowPos` rather than WPF `Left/Top`; on `DpiChanged` the overlay snaps back to its monitor. Monitor changes while running are not handled.
- **Click-through** is done by toggling `WS_EX_TRANSPARENT` on the overlay (`Native.SetExStyle`) when in Cursor mode or ink is hidden. The `InkCanvas` background is `#01000000` on purpose: alpha 1 keeps the layered window hit-testable while drawing. Overlays also get `WS_EX_TOOLWINDOW` to stay out of Alt+Tab.
- **Undo (`UndoManager`) spans all overlays.** It listens to `StrokesChanged` on every canvas and groups changes between `BeginGroup`/`EndGroup` (one mouse/stylus gesture, or one Clear) so they undo together. Changes it makes itself are hidden behind a `_suppress` flag. Anything that edits strokes programmatically and must not be undoable has to go through `RemoveWithoutHistory`.
- **`FadingInk` (one per overlay)** removes strokes `FadeSeconds` after they are drawn, fading alpha over the last 500 ms on a 33 ms `DispatcherTimer`. Expiry data lives in a `ConditionalWeakTable` that is kept after removal, so strokes restored by undo keep their countdown, and pieces left by the point eraser inherit the earliest expiry of the stroke they replaced. Expired strokes are also dropped from the undo history.
- **Global hotkeys** (`HotkeyManager`) use `RegisterHotKey` on a message-only `HwndSource`. Registration can fail if another app owns the combination; failures are reported via a tray balloon, not thrown.
- **Settings** (color, size, fade seconds, toolbar collapsed) are stored in `%AppData%\InkOverlay\settings.json`. They are loaded at startup and saved only in `App.OnExit`. Load/save errors are swallowed on purpose.
- The toolbar uses `ShowActivated="False"` and non-focusable buttons so clicking it does not steal focus. Icons are glyphs from `Segoe Fluent Icons` / `Segoe MDL2 Assets`, written as literal private-use characters in the XAML and code.
- `InkOverlay.csproj` turns on `Switch.System.Windows.Input.Stylus.EnablePointerSupport` (WM_POINTER stylus input) and suppresses `WFO0003`, since DPI awareness belongs in the manifest for a WPF app.
