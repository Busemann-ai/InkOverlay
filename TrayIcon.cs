using System;
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
        menu.Items.Add("Avslutt", null, (_, _) => app.Quit());

        _icon = new Forms.NotifyIcon
        {
            Icon = System.Drawing.SystemIcons.Application,
            Text = "InkOverlay",
            ContextMenuStrip = menu,
            Visible = true,
        };
        _icon.MouseClick += (_, e) =>
        {
            if (e.Button == Forms.MouseButtons.Left) app.ToggleDraw();
        };
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
