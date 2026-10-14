using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Interop;

namespace InkOverlay;

/// <summary>Transparent, topmost drawing surface covering one monitor.</summary>
public partial class OverlayWindow : Window
{
    readonly System.Drawing.Rectangle _bounds;
    readonly App _app;
    readonly FadingInk _fading;
    IntPtr _hwnd;

    public InkCanvas Ink => InkLayer;

    public OverlayWindow(System.Drawing.Rectangle bounds, App app)
    {
        InitializeComponent();
        _bounds = Native.InsetForAutoHideTaskbars(bounds);
        _app = app;

        var undo = app.UndoManager;
        undo.Track(InkLayer);
        InkLayer.PreviewMouseLeftButtonDown += (_, _) => undo.BeginGroup();
        InkLayer.PreviewMouseLeftButtonUp += (_, _) => undo.EndGroup();
        InkLayer.PreviewStylusDown += (_, _) => undo.BeginGroup();
        InkLayer.PreviewStylusUp += (_, _) => undo.EndGroup();
        _fading = new FadingInk(InkLayer, app.State, undo);

        PreviewKeyDown += (_, e) => app.HandleKey(e);
        SourceInitialized += OnSourceInitialized;
        Loaded += (_, _) => PlaceOnScreen();
        // WPF applies its own suggested rect on DPI change; snap back to the monitor afterwards.
        DpiChanged += (_, _) => Dispatcher.InvokeAsync(PlaceOnScreen);
        app.State.Changed += Apply;
    }

    void OnSourceInitialized(object? sender, EventArgs e)
    {
        _hwnd = new WindowInteropHelper(this).Handle;
        Native.SetExStyle(_hwnd, Native.WS_EX_TOOLWINDOW, true); // keep out of Alt+Tab
        PlaceOnScreen();
        Apply();
    }

    void PlaceOnScreen()
    {
        if (_hwnd == IntPtr.Zero) return;
        Native.SetWindowPos(_hwnd, Native.HWND_TOPMOST,
            _bounds.X, _bounds.Y, _bounds.Width, _bounds.Height, Native.SWP_NOACTIVATE);
    }

    void Apply()
    {
        var state = _app.State;
        InkLayer.Visibility = state.InkVisible ? Visibility.Visible : Visibility.Hidden;

        bool drawing = state.InkVisible && state.Tool != Tool.Cursor;
        if (_hwnd != IntPtr.Zero) Native.SetExStyle(_hwnd, Native.WS_EX_TRANSPARENT, !drawing);

        var attributes = new DrawingAttributes
        {
            Color = state.Color,
            Width = state.Size,
            Height = state.Size,
            FitToCurve = true,
            StylusTip = StylusTip.Ellipse,
        };

        switch (state.Tool)
        {
            case Tool.Pen:
                InkLayer.DefaultDrawingAttributes = attributes;
                InkLayer.EditingMode = InkCanvasEditingMode.Ink;
                break;
            case Tool.Highlighter:
                attributes.IsHighlighter = true;
                attributes.Width = attributes.Height = state.Size * 4;
                attributes.StylusTip = StylusTip.Rectangle;
                InkLayer.DefaultDrawingAttributes = attributes;
                InkLayer.EditingMode = InkCanvasEditingMode.Ink;
                break;
            case Tool.Eraser:
                InkLayer.EraserShape = new EllipseStylusShape(state.Size * 4, state.Size * 4);
                // The eraser cursor only refreshes when the editing mode changes.
                InkLayer.EditingMode = InkCanvasEditingMode.None;
                InkLayer.EditingMode = InkCanvasEditingMode.EraseByPoint;
                break;
            default:
                InkLayer.EditingMode = InkCanvasEditingMode.None;
                break;
        }
    }
}
