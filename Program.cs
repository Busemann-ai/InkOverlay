using System;
using Velopack;

namespace InkOverlay;

public static class Program
{
    [STAThread]
    public static void Main()
    {
        // Must run first: during install/uninstall Velopack starts the exe with special arguments,
        // runs the matching hook and exits here. It also applies an update downloaded in a previous session.
        VelopackApp.Build()
            .OnAfterInstallFastCallback(_ => Autostart.Enable())
            .OnBeforeUninstallFastCallback(_ => Autostart.Disable())
            .Run();

        var app = new App();
        app.InitializeComponent();
        app.Run();
    }
}
