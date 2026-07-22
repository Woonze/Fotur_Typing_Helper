using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Threading;

namespace FoturTypingHelper.App;

public sealed class ScreenEdgeOverlay : IDisposable
{
    private readonly List<Window> _edges = [];
    private readonly DispatcherTimer _pulse;
    private bool _rising;

    private ScreenEdgeOverlay()
    {
        var main = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        if (main is not null)
            foreach (var screen in main.Screens.All) AddScreen(screen);
        _pulse = new DispatcherTimer(TimeSpan.FromMilliseconds(45), DispatcherPriority.Background, (_, _) => Pulse());
        _pulse.Start();
    }

    public static ScreenEdgeOverlay ShowRecording() => new();

    private void AddScreen(Screen screen)
    {
        var b = screen.Bounds;
        var thickness = Math.Max(4, (int)Math.Round(6 * screen.Scaling));
        AddEdge(b.X, b.Y, b.Width, thickness, screen.Scaling);
        AddEdge(b.X, b.Bottom - thickness, b.Width, thickness, screen.Scaling);
        AddEdge(b.X, b.Y, thickness, b.Height, screen.Scaling);
        AddEdge(b.Right - thickness, b.Y, thickness, b.Height, screen.Scaling);
    }

    private void AddEdge(int x, int y, int width, int height, double scaling)
    {
        var window = new Window
        {
            Position = new PixelPoint(x, y), Width = width / scaling, Height = height / scaling,
            SystemDecorations = SystemDecorations.None, CanResize = false, ShowInTaskbar = false,
            ShowActivated = false, Focusable = false, Topmost = true, IsHitTestVisible = false,
            Background = new LinearGradientBrush
            {
                GradientStops = { new GradientStop(Color.Parse("#62FFF7"), 0), new GradientStop(Color.Parse("#FF5BDD"), 1) }
            }, Opacity = 0.82
        };
        window.Show(); _edges.Add(window);
    }

    private void Pulse()
    {
        foreach (var edge in _edges) edge.Opacity += _rising ? 0.025 : -0.025;
        if (_edges.Count > 0 && _edges[0].Opacity <= 0.52) _rising = true;
        if (_edges.Count > 0 && _edges[0].Opacity >= 0.94) _rising = false;
    }

    public void SetProcessing()
    {
        _pulse.Stop();
        foreach (var edge in _edges) { edge.Background = Brush.Parse("#FF5BDD"); edge.Opacity = 0.82; }
    }

    public void Dispose() { _pulse.Stop(); foreach (var edge in _edges) edge.Close(); _edges.Clear(); }
}
