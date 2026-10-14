using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Velopack;
using Forms = System.Windows.Forms;

namespace InkOverlay;

/// <summary>Composition root and command surface shared by overlays, toolbar, tray and hotkeys.</summary>
public partial class App : Application
{
    Mutex? _mutex;
    bool _ownsMutex;
    Settings _settings = new();
    readonly List<OverlayWindow> _overlays = new();
    ToolbarWindow? _toolbar;
    TrayIcon? _tray;
    HotkeyManager? _hotkeys;

    public AppState State { get; } = new();
    public UndoManager UndoManager { get; } = new();
    public Updater Updater { get; } = new();

    protected override void OnStartup(StartupEventArgs e)
    {
        _mutex = new Mutex(true, "InkOverlay.SingleInstance", out _ownsMutex);
        if (!_ownsMutex)
        {
            Shutdown();
            return;
        }

        base.OnStartup(e);

        // Asked before the topmost overlays exist, so the dialog cannot end up behind them.
        if (Program.IsFirstRun) AskAutostart();

        _settings = Settings.Load();
        State.Color = _settings.GetColor();
        State.Size = _settings.Size;
        State.FadeSeconds = _settings.FadeSeconds;

        // One overlay per monitor; Screen bounds are physical pixels because the app is per-monitor DPI aware.
        foreach (var screen in Forms.Screen.AllScreens.OrderByDescending(s => s.Primary))
        {
            var overlay = new OverlayWindow(screen.Bounds, this);
            _overlays.Add(overlay);
            overlay.Show();
        }

        _toolbar = new ToolbarWindow(this) { Owner = _overlays[0], IsCollapsed = _settings.ToolbarCollapsed };
        _toolbar.Show();

        _tray = new TrayIcon(this);

        _hotkeys = new HotkeyManager();
        var failed = new List<string>();
        void Register(ModifierKeys mods, Key key, string label, System.Action action)
        {
            if (!_hotkeys.Register(mods, key, action)) failed.Add(label);
        }
        Register(ModifierKeys.Control | ModifierKeys.Shift, Key.D, "Ctrl+Shift+D", ToggleDraw);
        Register(ModifierKeys.Control | ModifierKeys.Shift, Key.Z, "Ctrl+Shift+Z", Undo);
        Register(ModifierKeys.Control | ModifierKeys.Shift, Key.X, "Ctrl+Shift+X", Clear);
        Register(ModifierKeys.Control | ModifierKeys.Shift, Key.H, "Ctrl+Shift+H", ToggleInk);
        Register(ModifierKeys.Control | ModifierKeys.Shift, Key.Y, "Ctrl+Shift+Y", ToggleToolbar);
        if (failed.Count > 0)
            _tray.ShowInfo("Hurtigtaster i bruk av et annet program: " + string.Join(", ", failed));

        _ = DownloadUpdateInBackground();
    }

    async Task DownloadUpdateInBackground()
    {
        // VelopackApp in Program.Main applies a downloaded update on the next start.
        var info = await Updater.DownloadAsync(throwOnError: false);
        if (info is not null)
            _tray?.ShowInfo($"Versjon {info.TargetFullRelease.Version} er lastet ned og tas i bruk neste gang InkOverlay starter.");
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_ownsMutex)
        {
            SaveSettings();
            _hotkeys?.Dispose();
            _tray?.Dispose();
            _mutex?.ReleaseMutex();
        }
        _mutex?.Dispose();
        base.OnExit(e);
    }

    void SaveSettings()
    {
        _settings.Color = State.Color.ToString();
        _settings.Size = State.Size;
        _settings.FadeSeconds = State.FadeSeconds;
        _settings.ToolbarCollapsed = _toolbar?.IsCollapsed ?? false;
        _settings.Save();
    }

    public void ToggleDraw()
    {
        if (!State.InkVisible) State.InkVisible = true;
        State.ToggleDrawMode();
        if (State.Tool != Tool.Cursor) _overlays[0].Activate();
    }

    public void SetTool(Tool tool)
    {
        if (tool != Tool.Cursor) State.InkVisible = true;
        State.Tool = tool;
    }

    public void SetColor(Color color)
    {
        State.Color = color;
        if (State.Tool != Tool.Highlighter) SetTool(Tool.Pen);
    }

    public void ChangeSize(int delta) => State.ChangeSize(delta);

    public void Undo() => UndoManager.Undo();

    public void Clear() => UndoManager.Clear(_overlays.Select(o => o.Ink));

    public void ToggleInk() => State.InkVisible = !State.InkVisible;

    public void SetFade(int seconds) => State.FadeSeconds = seconds;

    public void CycleFade(int step = 1) => State.CycleFade(step);

    public void HideToolbar()
    {
        _toolbar?.Hide();
        _tray?.ShowInfo("Verktøylinjen er skjult. Vis den igjen med Ctrl+Shift+Y eller fra ikonet i statusfeltet.");
    }

    public void ToggleToolbar()
    {
        if (_toolbar is null) return;
        if (_toolbar.IsVisible) HideToolbar();
        else _toolbar.Show();
    }

    static void AskAutostart()
    {
        var answer = MessageBox.Show(
            "Vil du at InkOverlay skal starte automatisk når du logger på Windows?\n\n" +
            "Du kan endre dette senere under «Start med Windows» i menyen i statusfeltet.",
            "InkOverlay", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (answer == MessageBoxResult.Yes) Autostart.Enable();
    }

    public void ToggleAutostart()
    {
        if (Autostart.IsEnabled) Autostart.Disable();
        else Autostart.Enable();
    }

    public async void CheckForUpdates()
    {
        UpdateInfo? info;
        try
        {
            info = await Updater.DownloadAsync(throwOnError: true);
        }
        catch (System.Exception)
        {
            _tray?.ShowInfo("Fant ikke ut om det finnes en ny versjon. Sjekk nettforbindelsen og prøv igjen.");
            return;
        }
        if (info is null)
        {
            _tray?.ShowInfo($"Du har nyeste versjon ({Updater.Version}).");
            return;
        }
        // ApplyUpdatesAndRestart exits the process without running OnExit.
        SaveSettings();
        Updater.ApplyAndRestart(info);
    }

    public void Quit() => Shutdown();

    /// <summary>Local shortcuts, active when an overlay or the toolbar has keyboard focus.</summary>
    public void HandleKey(KeyEventArgs e)
    {
        bool ctrl = (Keyboard.Modifiers & ModifierKeys.Control) != 0;
        switch (e.Key)
        {
            case Key.Z when ctrl: Undo(); break;
            case Key.Escape: SetTool(Tool.Cursor); break;
            case Key.P: SetTool(Tool.Pen); break;
            case Key.H: SetTool(Tool.Highlighter); break;
            case Key.E: SetTool(Tool.Eraser); break;
            case Key.F: CycleFade(); break;
            case Key.Delete: Clear(); break;
            case Key.OemPlus or Key.Add: ChangeSize(+1); break;
            case Key.OemMinus or Key.Subtract: ChangeSize(-1); break;
            case >= Key.D1 and <= Key.D6: SetColor(AppState.Palette[e.Key - Key.D1]); break;
            default: return;
        }
        e.Handled = true;
    }
}
