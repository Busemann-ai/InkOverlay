using System;
using System.Drawing;
using Forms = System.Windows.Forms;

namespace InkOverlay;

public sealed class TrayIcon : IDisposable
{
    readonly Forms.NotifyIcon _icon;

    public TrayIcon(App app)
    {
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("Tegn / pek\tCtrl+Shift+D", null, (_, _) => app.ToggleDraw());
        menu.Items.Add("Vis / skjul blekk\tCtrl+Shift+H", null, (_, _) => app.ToggleInk());
        menu.Items.Add("Angre\tCtrl+Shift+Z", null, (_, _) => app.Undo());
        menu.Items.Add("Tøm skjermen\tCtrl+Shift+X", null, (_, _) => app.Clear());
        menu.Items.Add("Vis / skjul verktøylinje\tCtrl+Shift+Y", null, (_, _) => app.ToggleToolbar());
        var fade = new Forms.ToolStripMenuItem("Fjern tegning etter");
        foreach (var seconds in AppState.FadeOptions)
        {
            var item = new Forms.ToolStripMenuItem(seconds > 0 ? $"{seconds} sekunder" : "Aldri") { Tag = seconds };
            item.Click += (_, _) => app.SetFade(seconds);
            fade.DropDownItems.Add(item);
        }
        menu.Opening += (_, _) =>
        {
            foreach (Forms.ToolStripMenuItem item in fade.DropDownItems)
                item.Checked = (int)item.Tag! == app.State.FadeSeconds;
        };
        menu.Items.Add(fade);
        menu.Items.Add(new Forms.ToolStripSeparator());
        // Only offered when installed, so a dotnet run copy never registers itself.
        if (app.Updater.IsInstalled)
        {
            var autostart = new Forms.ToolStripMenuItem("Start med Windows");
            autostart.Click += (_, _) => app.ToggleAutostart();
            menu.Opening += (_, _) => autostart.Checked = Autostart.IsEnabled;
            menu.Items.Add(autostart);
            menu.Items.Add("Se etter oppdateringer", null, (_, _) => app.CheckForUpdates());
            menu.Items.Add(new Forms.ToolStripSeparator());
        }
        menu.Items.Add("Om InkOverlay", null, (_, _) => app.ShowAbout());
        menu.Items.Add("Avslutt", null, (_, _) => app.Quit());

        _icon = new Forms.NotifyIcon
        {
            Icon = LoadIcon(),
            Text = $"InkOverlay {app.Updater.Version}",
            ContextMenuStrip = menu,
            Visible = true,
        };
        _icon.MouseClick += (_, e) =>
        {
            if (e.Button == Forms.MouseButtons.Left) app.ToggleDraw();
        };
    }

    static Icon LoadIcon()
    {
        // Embedded only when Assets\InkOverlay.ico exists at build time.
        using var stream = typeof(TrayIcon).Assembly.GetManifestResourceStream("InkOverlay.ico");
        return stream is null ? SystemIcons.Application : new Icon(stream, Forms.SystemInformation.SmallIconSize);
    }

    public void ShowInfo(string text) =>
        _icon.ShowBalloonTip(4000, "InkOverlay", text, Forms.ToolTipIcon.Info);

    public void Dispose()
    {
        _icon.Visible = false;
        _icon.ContextMenuStrip?.Dispose();
        _icon.Dispose();
    }
}
