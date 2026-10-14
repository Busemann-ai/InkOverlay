using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace InkOverlay;

public partial class ToolbarWindow : Window
{
    static readonly Brush Accent = new SolidColorBrush(Color.FromRgb(0x3B, 0x82, 0xF6));
    static readonly Brush FadeOn = new SolidColorBrush(Color.FromArgb(0x55, 0x3B, 0x82, 0xF6));
    static readonly FontFamily TextFont = new("Segoe UI");

    readonly App _app;
    readonly Dictionary<Tool, Button> _toolButtons;
    readonly List<(Button Button, Color Color)> _colorButtons = new();
    bool _collapsed;

    public ToolbarWindow(App app)
    {
        InitializeComponent();
        _app = app;

        _toolButtons = new()
        {
            [Tool.Cursor] = CursorButton,
            [Tool.Pen] = PenButton,
            [Tool.Highlighter] = HighlighterButton,
            [Tool.Eraser] = EraserButton,
        };
        foreach (var (tool, button) in _toolButtons)
            button.Click += (_, _) => app.SetTool(tool);

        for (int i = 0; i < AppState.Palette.Length; i++)
        {
            var color = AppState.Palette[i];
            var button = new Button
            {
                Style = (Style)FindResource("ColorButton"),
                Background = new SolidColorBrush(color),
                ToolTip = $"Farge ({i + 1})",
            };
            button.Click += (_, _) => app.SetColor(color);
            ColorPanel.Children.Add(button);
            _colorButtons.Add((button, color));
        }

        CollapseButton.Click += (_, _) => IsCollapsed = !IsCollapsed;
        HideButton.Click += (_, _) => app.HideToolbar();
        SmallerButton.Click += (_, _) => app.ChangeSize(-1);
        LargerButton.Click += (_, _) => app.ChangeSize(+1);
        FadeButton.Click += (_, _) => app.CycleFade();
        FadeButton.MouseRightButtonUp += (_, e) => { app.CycleFade(-1); e.Handled = true; };
        UndoButton.Click += (_, _) => app.Undo();
        ClearButton.Click += (_, _) => app.Clear();
        InkButton.Click += (_, _) => app.ToggleInk();
        CloseButton.Click += (_, _) => app.Quit();

        Grip.MouseLeftButtonDown += (_, _) => DragMove();
        PreviewKeyDown += (_, e) => app.HandleKey(e);
        Loaded += (_, _) =>
        {
            var area = SystemParameters.WorkArea;
            Left = area.Left + 16;
            Top = area.Top + (area.Height - ActualHeight) / 2;
        };

        app.State.Changed += Apply;
        Apply();
        ApplyCollapsed();
    }

    /// <summary>Collapsed shows only the grip and the header buttons.</summary>
    public bool IsCollapsed
    {
        get => _collapsed;
        set
        {
            _collapsed = value;
            ApplyCollapsed();
        }
    }

    void ApplyCollapsed()
    {
        Body.Visibility = _collapsed ? Visibility.Collapsed : Visibility.Visible;
        CollapseButton.Content = _collapsed ? "" : "";
        CollapseButton.ToolTip = _collapsed ? "Vis hele verktøylinjen" : "Minimer verktøylinjen";
    }

    void Apply()
    {
        var state = _app.State;
        foreach (var (tool, button) in _toolButtons)
            button.Background = state.Tool == tool ? Accent : Brushes.Transparent;
        foreach (var (button, color) in _colorButtons)
            button.BorderBrush = state.Color == color ? Brushes.White : Brushes.Transparent;
        SizeText.Text = state.Size.ToString("0");

        var fade = new TextBlock();
        fade.Inlines.Add(new Run(""));
        if (state.FadeSeconds > 0)
            fade.Inlines.Add(new Run($" {state.FadeSeconds}") { FontFamily = TextFont, FontSize = 11 });
        FadeButton.Content = fade;
        FadeButton.Background = state.FadeSeconds > 0 ? FadeOn : Brushes.Transparent;

        InkButton.Content = state.InkVisible ? "" : "";
        InkButton.ToolTip = (state.InkVisible ? "Skjul blekk" : "Vis blekk") + " (Ctrl+Shift+H)";
    }
}
