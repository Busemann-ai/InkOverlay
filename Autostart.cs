using System;
using Microsoft.Win32;

namespace InkOverlay;

/// <summary>Start with Windows via the per-user Run key.</summary>
public static class Autostart
{
    const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    const string ValueName = "InkOverlay";

    public static bool IsEnabled
    {
        get
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKey);
                return key?.GetValue(ValueName) is string;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }

    public static void Enable()
    {
        // Velopack installs to %LocalAppData%\InkOverlay\current, which stays the same across updates.
        if (Environment.ProcessPath is not { } exe) return;
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(RunKey);
            key.SetValue(ValueName, $"\"{exe}\"");
        }
        catch (Exception)
        {
            // Autostart is a convenience; failing to set it is not worth an error.
        }
    }

    public static void Disable()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
            key?.DeleteValue(ValueName, throwOnMissingValue: false);
        }
        catch (Exception)
        {
            // See Enable.
        }
    }
}
