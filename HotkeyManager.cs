using System;
using System.Collections.Generic;
using System.Windows.Input;
using System.Windows.Interop;

namespace InkOverlay;

/// <summary>System-wide hotkeys via RegisterHotKey on a message-only window.</summary>
public sealed class HotkeyManager : IDisposable
{
    static readonly IntPtr HWND_MESSAGE = new(-3);

    readonly HwndSource _source;
    readonly Dictionary<int, Action> _actions = new();
    int _nextId = 1;

    public HotkeyManager()
    {
        _source = new HwndSource(new HwndSourceParameters("InkOverlayHotkeys")
        {
            Width = 0,
            Height = 0,
            WindowStyle = 0,
            ParentWindow = HWND_MESSAGE,
        });
        _source.AddHook(WndProc);
    }

    /// <returns>false if another application already owns the combination.</returns>
    public bool Register(ModifierKeys modifiers, Key key, Action action)
    {
        int id = _nextId++;
        // WPF ModifierKeys share their bit values with MOD_ALT/MOD_CONTROL/MOD_SHIFT/MOD_WIN.
        uint mods = (uint)modifiers | Native.MOD_NOREPEAT;
        uint vk = (uint)KeyInterop.VirtualKeyFromKey(key);
        if (!Native.RegisterHotKey(_source.Handle, id, mods, vk)) return false;
        _actions[id] = action;
        return true;
    }

    IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == Native.WM_HOTKEY && _actions.TryGetValue(wParam.ToInt32(), out var action))
        {
            action();
            handled = true;
        }
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        foreach (int id in _actions.Keys) Native.UnregisterHotKey(_source.Handle, id);
        _actions.Clear();
        _source.Dispose();
    }
}
