using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Media;
using System.Windows.Threading;

namespace InkOverlay;

/// <summary>
/// Removes strokes a set number of seconds after they are drawn (<see cref="AppState.FadeSeconds"/>),
/// fading them out over the last half second. Faded strokes are also dropped from the undo history.
/// </summary>
public sealed class FadingInk
{
    static readonly TimeSpan FadeOut = TimeSpan.FromMilliseconds(500);

    sealed record Expiry(DateTime At, byte Alpha);

    readonly InkCanvas _canvas;
    readonly UndoManager _undo;
    // Kept after removal so a stroke restored by undo, or split by the eraser, keeps its countdown.
    readonly ConditionalWeakTable<Stroke, Expiry> _expiries = new();
    readonly List<Stroke> _active = new();
    readonly DispatcherTimer _timer;

    public FadingInk(InkCanvas canvas, AppState state, UndoManager undo)
    {
        _canvas = canvas;
        _undo = undo;
        _timer = new DispatcherTimer(TimeSpan.FromMilliseconds(33), DispatcherPriority.Render, (_, _) => Tick(), canvas.Dispatcher);
        _timer.Stop();

        canvas.StrokeCollected += (_, e) =>
        {
            if (state.FadeSeconds <= 0) return;
            _expiries.AddOrUpdate(e.Stroke, new Expiry(
                DateTime.UtcNow.AddSeconds(state.FadeSeconds), e.Stroke.DrawingAttributes.Color.A));
            Start(e.Stroke);
        };
        canvas.Strokes.StrokesChanged += OnStrokesChanged;
    }

    void OnStrokesChanged(object? sender, StrokeCollectionChangedEventArgs e)
    {
        Expiry? inherited = null;
        foreach (var s in e.Removed)
        {
            if (!_expiries.TryGetValue(s, out var expiry)) continue;
            _active.Remove(s);
            if (inherited == null || expiry.At < inherited.At) inherited = expiry;
        }
        foreach (var s in e.Added)
        {
            if (_expiries.TryGetValue(s, out _)) Start(s);
            else if (inherited != null)
            {
                // The point eraser replaces a stroke with its remaining pieces.
                _expiries.AddOrUpdate(s, inherited);
                Start(s);
            }
        }
    }

    void Start(Stroke stroke)
    {
        if (!_active.Contains(stroke)) _active.Add(stroke);
        _timer.Start();
    }

    void Tick()
    {
        var now = DateTime.UtcNow;
        for (int i = _active.Count - 1; i >= 0; i--)
        {
            var stroke = _active[i];
            if (!_expiries.TryGetValue(stroke, out var expiry)) { _active.RemoveAt(i); continue; }

            var remaining = expiry.At - now;
            if (remaining <= TimeSpan.Zero)
            {
                _active.RemoveAt(i);
                _undo.RemoveWithoutHistory(_canvas, stroke);
            }
            else if (remaining < FadeOut)
            {
                var c = stroke.DrawingAttributes.Color;
                var alpha = (byte)(expiry.Alpha * (remaining / FadeOut));
                if (c.A != alpha) stroke.DrawingAttributes.Color = Color.FromArgb(alpha, c.R, c.G, c.B);
            }
        }
        if (_active.Count == 0) _timer.Stop();
    }
}
