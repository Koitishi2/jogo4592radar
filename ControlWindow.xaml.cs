using System;
using System.Windows;

namespace Jogo4592Radar;

public partial class ControlWindow : Window
{
    private OverlayWindow? _overlay;
    private bool _running = true;
    private bool _locked;

    public ControlWindow()
    {
        InitializeComponent();
        var version = typeof(App).Assembly.GetName().Version?.ToString(3) ?? "1.0.0";
        Title = $"jogo4592radar {version} — Simulação";
        VersionHeading.Text = $"jogo4592radar {version}";
        Loaded += (_, _) =>
        {
            _overlay = new OverlayWindow
            {
                Left = SystemParameters.WorkArea.Right - 270,
                Top = SystemParameters.WorkArea.Bottom - 270
            };
            _overlay.SetSize(SizeSlider.Value);
            _overlay.SetShade(OpacitySlider.Value);
            _overlay.Show();
        };
        Closed += (_, _) => _overlay?.Close();
    }

    private void OpacityChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (OpacityText is null) return;
        OpacityText.Text = $"{Math.Round(e.NewValue * 100)}%";
        _overlay?.SetShade(e.NewValue);
    }

    private void OverlaySizeChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (SizeText is null) return;
        SizeText.Text = $"{Math.Round(e.NewValue)} px";
        _overlay?.SetSize(e.NewValue);
    }

    private void ToggleSimulation(object sender, RoutedEventArgs e)
    {
        _running = !_running;
        _overlay?.SetSimulation(_running);
        ToggleButton.Content = _running ? "Pausar simulação" : "Retomar simulação";
    }

    private void ToggleLock(object sender, RoutedEventArgs e)
    {
        _locked = !_locked;
        _overlay?.SetLocked(_locked);
        LockButton.Content = _locked ? "Destravar posição" : "Travar posição";
    }
}
