using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Threading;

namespace FoturTypingHelper.App;

/// <summary>A non-interactive, brand-coloured glow occupying at most 5% of each display.</summary>
public sealed class ScreenEdgeOverlay : IDisposable
{
    private static readonly Color Cyan = Color.Parse("#5DFFF4");
    private static readonly Color Violet = Color.Parse("#8D6BFF");
    private static readonly Color Magenta = Color.Parse("#FF5BDD");
    private readonly List<Window> _windows = [];
    private readonly DispatcherTimer _pulse;
    private bool _rising;

    private ScreenEdgeOverlay()
    {
        var main = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        if (main is not null)
            foreach (var screen in main.Screens.All) AddScreen(screen);
        _pulse = new DispatcherTimer(TimeSpan.FromMilliseconds(55), DispatcherPriority.Background, (_, _) => Pulse());
        _pulse.Start();
    }

    public static ScreenEdgeOverlay ShowRecording() => new();

    private void AddScreen(Screen screen)
    {
        var bounds = screen.Bounds;
        var width = bounds.Width / screen.Scaling;
        var height = bounds.Height / screen.Scaling;
        // The complete fade never reaches farther than five percent into the display.
        var shortestSide = Math.Min(width, height);
        var depth = Math.Min(shortestSide * 0.05, Math.Clamp(shortestSide * 0.045, 18, 68));
        var canvas = new Grid { Background = Brushes.Transparent };

        canvas.Children.Add(Edge(depth, HorizontalAlignment.Stretch, VerticalAlignment.Top,
            BrandBrush(horizontal: true), FadeMask(EdgeSide.Top)));
        canvas.Children.Add(Edge(depth, HorizontalAlignment.Stretch, VerticalAlignment.Bottom,
            BrandBrush(horizontal: true), FadeMask(EdgeSide.Bottom)));
        canvas.Children.Add(Edge(depth, HorizontalAlignment.Left, VerticalAlignment.Stretch,
            BrandBrush(horizontal: false), FadeMask(EdgeSide.Left)));
        canvas.Children.Add(Edge(depth, HorizontalAlignment.Right, VerticalAlignment.Stretch,
            BrandBrush(horizontal: false), FadeMask(EdgeSide.Right)));

        // A fine luminous contour keeps the soft glow visually crisp at the physical edge.
        canvas.Children.Add(new Border
        {
            BorderThickness = new Thickness(2.2),
            BorderBrush = BrandBrush(horizontal: true),
            IsHitTestVisible = false
        });

        var window = new Window
        {
            Position = new PixelPoint(bounds.X, bounds.Y), Width = width, Height = height,
            SystemDecorations = SystemDecorations.None, CanResize = false, ShowInTaskbar = false,
            ShowActivated = false, Focusable = false, Topmost = true, IsHitTestVisible = false,
            Background = Brushes.Transparent,
            TransparencyLevelHint = [WindowTransparencyLevel.Transparent],
            Content = canvas,
            Opacity = 0.78
        };
        window.Show();
        _windows.Add(window);
    }

    private static Border Edge(double depth, HorizontalAlignment horizontal, VerticalAlignment vertical,
        IBrush background, IBrush mask) => new()
    {
        Width = horizontal == HorizontalAlignment.Stretch ? double.NaN : depth,
        Height = vertical == VerticalAlignment.Stretch ? double.NaN : depth,
        HorizontalAlignment = horizontal,
        VerticalAlignment = vertical,
        Background = background,
        OpacityMask = mask,
        BoxShadow = new BoxShadows(new BoxShadow
        {
            Blur = depth * 0.48,
            Spread = 0,
            IsInset = true,
            Color = Color.FromArgb(95, Violet.R, Violet.G, Violet.B)
        }),
        IsHitTestVisible = false
    };

    private static LinearGradientBrush BrandBrush(bool horizontal) => new()
    {
        StartPoint = Point(horizontal ? 0 : 0.5, horizontal ? 0.5 : 0),
        EndPoint = Point(horizontal ? 1 : 0.5, horizontal ? 0.5 : 1),
        GradientStops =
        {
            new GradientStop(Cyan, 0),
            new GradientStop(Violet, 0.5),
            new GradientStop(Magenta, 1)
        }
    };

    private static LinearGradientBrush FadeMask(EdgeSide side)
    {
        var (start, end) = side switch
        {
            EdgeSide.Top => (Point(0.5, 0), Point(0.5, 1)),
            EdgeSide.Bottom => (Point(0.5, 1), Point(0.5, 0)),
            EdgeSide.Left => (Point(0, 0.5), Point(1, 0.5)),
            _ => (Point(1, 0.5), Point(0, 0.5))
        };
        return new LinearGradientBrush
        {
            StartPoint = start,
            EndPoint = end,
            GradientStops =
            {
                new GradientStop(Color.FromArgb(235, 255, 255, 255), 0),
                new GradientStop(Color.FromArgb(100, 255, 255, 255), 0.28),
                new GradientStop(Color.FromArgb(28, 255, 255, 255), 0.68),
                new GradientStop(Colors.Transparent, 1)
            }
        };
    }

    private static RelativePoint Point(double x, double y) => new(x, y, RelativeUnit.Relative);

    private void Pulse()
    {
        foreach (var window in _windows) window.Opacity += _rising ? 0.012 : -0.012;
        if (_windows.Count > 0 && _windows[0].Opacity <= 0.68) _rising = true;
        if (_windows.Count > 0 && _windows[0].Opacity >= 0.88) _rising = false;
    }

    public void SetProcessing()
    {
        _pulse.Stop();
        foreach (var window in _windows) window.Opacity = 0.72;
    }

    public void Dispose()
    {
        _pulse.Stop();
        foreach (var window in _windows) window.Close();
        _windows.Clear();
    }

    private enum EdgeSide { Top, Bottom, Left, Right }
}
