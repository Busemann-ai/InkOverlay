using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Ink;

namespace InkOverlay;

/// <summary>
/// Records stroke additions/removals across all overlays. Changes made between
/// <see cref="BeginGroup"/> and <see cref="EndGroup"/> (one pen-down gesture, or one clear)
/// are undone together.
/// </summary>
public sealed class UndoManager
{
    sealed class Change
    {
        public readonly StrokeCollection Added = new();
        public readonly StrokeCollection Removed = new();
        public bool IsEmpty => Added.Count == 0 && Removed.Count == 0;
    }

    sealed class Group
    {
        public readonly Dictionary<InkCanvas, Change> Changes = new();
        public bool IsEmpty => Changes.Values.All(c => c.IsEmpty);
    }

    readonly Stack<Group> _undo = new();
    Group? _open;
    bool _suppress;

    public void Track(InkCanvas canvas) =>
        canvas.Strokes.StrokesChanged += (_, e) => Record(canvas, e);

    public void BeginGroup()
    {
        EndGroup();
        _open = new Group();
    }

    public void EndGroup()
    {
        var group = _open;
        _open = null;
        if (group != null) Push(group);
    }

    public void Undo()
    {
        EndGroup();
        if (_undo.Count == 0) return;

        var group = _undo.Pop();
        _suppress = true;
        try
        {
            foreach (var (canvas, change) in group.Changes)
            {
                foreach (var s in change.Added)
                    if (canvas.Strokes.Contains(s)) canvas.Strokes.Remove(s);
                foreach (var s in change.Removed)
                    if (!canvas.Strokes.Contains(s)) canvas.Strokes.Add(s);
            }
        }
        finally
        {
            _suppress = false;
        }
    }

    public void Clear(IEnumerable<InkCanvas> canvases)
    {
        BeginGroup();
        foreach (var canvas in canvases) canvas.Strokes.Clear();
        EndGroup();
    }

    /// <summary>Removes a stroke for good: it is not recorded, and undo will never bring it back.</summary>
    public void RemoveWithoutHistory(InkCanvas canvas, Stroke stroke)
    {
        _suppress = true;
        try
        {
            if (canvas.Strokes.Contains(stroke)) canvas.Strokes.Remove(stroke);
        }
        finally
        {
            _suppress = false;
        }

        foreach (var group in _open == null ? _undo : _undo.Append(_open))
        {
            if (!group.Changes.TryGetValue(canvas, out var change)) continue;
            if (change.Added.Contains(stroke)) change.Added.Remove(stroke);
            if (change.Removed.Contains(stroke)) change.Removed.Remove(stroke);
        }

        // Stack enumerates top first; rebuild bottom-up without the groups that became empty.
        var kept = _undo.Where(g => !g.IsEmpty).Reverse().ToList();
        _undo.Clear();
        foreach (var group in kept) _undo.Push(group);
    }

    void Record(InkCanvas canvas, StrokeCollectionChangedEventArgs e)
    {
        if (_suppress) return;

        var group = _open ?? new Group();
        if (!group.Changes.TryGetValue(canvas, out var change))
            group.Changes[canvas] = change = new Change();

        foreach (var s in e.Removed)
        {
            // A stroke created and erased within the same gesture leaves no trace.
            if (change.Added.Contains(s)) change.Added.Remove(s);
            else if (!change.Removed.Contains(s)) change.Removed.Add(s);
        }
        foreach (var s in e.Added)
            if (!change.Added.Contains(s)) change.Added.Add(s);

        if (_open == null) Push(group);
    }

    void Push(Group group)
    {
        if (!group.IsEmpty) _undo.Push(group);
    }
}
