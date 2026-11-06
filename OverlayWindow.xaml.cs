using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Jogo4592Radar;

public partial class OverlayWindow : Window
{
    private const int GwlExstyle = -20;
    private const int WsExTransparent = 0x20;
    private const int WsExLayered = 0x80000;
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(80) };
    private bool _locked;
    private double _phase;

    public OverlayWindow()
    {
        InitializeComponent();
        _timer.Tick += (_, _) => { _phase += 0.035; DrawMarkers(); };
        _timer.Start();
        SizeChanged += (_, _) => DrawMarkers();
        Closed += (_, _) => _timer.Stop();
    }

    public void SetSize(double size) => Width = Height = Math.Round(size);
    public void SetShade(double opacity) => Shade.Opacity = Math.Clamp(opacity, 0, 0.75);

    public void SetSimulation(bool enabled)
    {
        if (enabled) _timer.Start();
        else { _timer.Stop(); MarkerCanvas.Children.Clear(); }
    }

    public void SetLocked(bool locked)
    {
        _locked = locked;
        Shade.Stroke = locked ? Brushes.Transparent : new SolidColorBrush(Color.FromRgb(255, 85, 85));
        var handle = new WindowInteropHelper(this).Handle;
        if (handle == IntPtr.Zero) return;
        var style = GetWindowLongPtr(handle, GwlExstyle).ToInt64();
        style = locked ? style | WsExTransparent | WsExLayered : style & ~WsExTransparent;
        SetWindowLongPtr(handle, GwlExstyle, new IntPtr(style));
    }

    private void DragOverlay(object sender, MouseButtonEventArgs e)
    {
        if (!_locked && e.LeftButton == MouseButtonState.Pressed) DragMove();
    }

    private void DrawMarkers()
    {
        if (ActualWidth <= 0 || ActualHeight <= 0) return;
        MarkerCanvas.Children.Clear();
        // Fixed normalized tracks, deliberately synthetic. No game or network data enters this method.
        var tracks = new (double x, double y)[]
        {
            (0.50 + 0.27 * Math.Cos(_phase), 0.50 + 0.27 * Math.Sin(_phase)),
            (0.50 + 0.33 * Math.Cos(_phase * 0.7 + 2.2), 0.50 + 0.33 * Math.Sin(_phase * 0.7 + 2.2)),
            (0.50 + 0.19 * Math.Cos(_phase * 1.2 + 4.1), 0.50 + 0.19 * Math.Sin(_phase * 1.2 + 4.1))
        };
        foreach (var (x, y) in tracks)
        {
            var marker = new Ellipse { Width = 12, Height = 12, Fill = Brushes.Red, Stroke = Brushes.White, StrokeThickness = 1.5 };
            Canvas.SetLeft(marker, x * ActualWidth - 6);
            Canvas.SetTop(marker, y * ActualHeight - 6);
            MarkerCanvas.Children.Add(marker);
        }
    }

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int index, IntPtr value);
}
