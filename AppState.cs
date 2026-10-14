using System;
using System.Windows.Media;

namespace InkOverlay;

public enum Tool { Cursor, Pen, Highlighter, Eraser }

/// <summary>Current tool and brush settings; every window re-applies itself on <see cref="Changed"/>.</summary>
public sealed class AppState
{
    public static readonly Color[] Palette =
    {
        Color.FromRgb(0xE5, 0x39, 0x35), // red
        Color.FromRgb(0xFD, 0xD8, 0x35), // yellow
        Color.FromRgb(0x43, 0xA0, 0x47), // green
        Color.FromRgb(0x1E, 0x88, 0xE5), // blue
        Color.FromRgb(0x11, 0x11, 0x11), // black
        Color.FromRgb(0xFF, 0xFF, 0xFF), // white
    };

    public const double MinSize = 1, MaxSize = 40;

    /// <summary>Seconds before new ink disappears; 0 keeps it.</summary>
    public static readonly int[] FadeOptions = { 0, 3, 5, 10, 15 };

    Tool _tool = Tool.Cursor;
    Tool _lastDrawTool = Tool.Pen;
    Color _color = Palette[0];
    double _size = 4;
    bool _inkVisible = true;
    int _fadeSeconds;

    public event Action? Changed;

    public Tool Tool
    {
        get => _tool;
        set
        {
            if (_tool == value) return;
            _tool = value;
            if (value != Tool.Cursor) _lastDrawTool = value;
            Changed?.Invoke();
        }
    }

    public Color Color
    {
        get => _color;
        set { if (_color == value) return; _color = value; Changed?.Invoke(); }
    }

    public double Size
    {
        get => _size;
        set
        {
            value = Math.Clamp(value, MinSize, MaxSize);
            if (_size == value) return;
            _size = value;
            Changed?.Invoke();
        }
    }

    public bool InkVisible
    {
        get => _inkVisible;
        set { if (_inkVisible == value) return; _inkVisible = value; Changed?.Invoke(); }
    }

    public int FadeSeconds
    {
        get => _fadeSeconds;
        set
        {
            if (Array.IndexOf(FadeOptions, value) < 0) value = 0;
            if (_fadeSeconds == value) return;
            _fadeSeconds = value;
            Changed?.Invoke();
        }
    }

    public void CycleFade(int step)
    {
        int n = FadeOptions.Length;
        int i = Array.IndexOf(FadeOptions, _fadeSeconds);
        FadeSeconds = FadeOptions[((i + step) % n + n) % n];
    }

    public void ToggleDrawMode() => Tool = Tool == Tool.Cursor ? _lastDrawTool : Tool.Cursor;

    public void ChangeSize(int delta) => Size += delta;
}
