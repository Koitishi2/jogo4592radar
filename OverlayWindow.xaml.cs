using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace Jogo4592Radar;

public partial class OverlayWindow : Window
{
    private const int GwlExstyle = -20;
    private const int WsExTransparent = 0x20;
    private const int WsExLayered = 0x80000;
    private bool _locked;
    private bool _playerAlert;

    public OverlayWindow()
    {
        InitializeComponent();
    }

    public void SetSize(double width, double heightRatio)
    {
        Width = Math.Round(width);
        Height = Math.Round(width * Math.Clamp(heightRatio, 0.6, 1.2));
    }
    public void SetShade(double opacity) => Shade.Opacity = Math.Clamp(opacity, 0, 0.75);

    internal void SetResources(IReadOnlyList<ResourcePoint> resources) => ResourceMarkers.SetPoints(resources);

    internal void SetPlayerAlert(bool active)
    {
        if (_playerAlert == active) return;
        _playerAlert = active;
        UpdateOutline();
    }

    public void SetLocked(bool locked)
    {
        _locked = locked;
        UpdateOutline();
        var handle = new WindowInteropHelper(this).Handle;
        if (handle == IntPtr.Zero) return;
        var style = GetWindowLongPtr(handle, GwlExstyle).ToInt64();
        style = locked ? style | WsExTransparent | WsExLayered : style & ~WsExTransparent;
        SetWindowLongPtr(handle, GwlExstyle, new IntPtr(style));
    }

    private void UpdateOutline() => Outline.Stroke = _playerAlert
        ? Brushes.OrangeRed
        : _locked ? Brushes.Transparent : Brushes.LightGreen;

    private void DragOverlay(object sender, MouseButtonEventArgs e)
    {
        if (!_locked && e.LeftButton == MouseButtonState.Pressed) DragMove();
    }

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int index, IntPtr value);
}
