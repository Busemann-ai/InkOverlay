using System;
using Velopack;

namespace InkOverlay;

public static class Program
{
    /// <summary>True on the first start right after installing (not after an update).</summary>
    public static bool IsFirstRun { get; private set; }

    [STAThread]
    public static void Main()
    {
        // Must run first: during install/uninstall Velopack starts the exe with special arguments,
        // runs the matching hook and exits here. It also applies an update downloaded in a previous session.
        // Autostart is not enabled on install: App asks on the first run instead.
        VelopackApp.Build()
            .OnFirstRun(_ => IsFirstRun = true)
            .OnBeforeUninstallFastCallback(_ => Autostart.Disable())
            .Run();

        var app = new App();
        app.InitializeComponent();
        app.Run();
    }
}
