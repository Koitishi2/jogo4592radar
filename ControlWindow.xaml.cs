using System;
using System.Windows;
using System.Windows.Threading;

namespace Jogo4592Radar;

public partial class ControlWindow : Window
{
    private OverlayWindow? _overlay;
    private bool _locked;
    private readonly NpcapMonitor _capture = new();
    private readonly DispatcherTimer _statusTimer = new() { Interval = TimeSpan.FromMilliseconds(750) };

    public ControlWindow()
    {
        InitializeComponent();
        var version = typeof(App).Assembly.GetName().Version?.ToString(3) ?? "1.0.0";
        Title = $"jogo4592radar {version} — Diagnóstico";
        VersionHeading.Text = $"jogo4592radar {version}";
        _statusTimer.Tick += (_, _) => UpdateCaptureStatus();
        _statusTimer.Start();
        Loaded += (_, _) =>
        {
            _overlay = new OverlayWindow();
            ApplyMinimapPreset();
            _overlay.SetShade(OpacitySlider.Value);
            _overlay.Show();
        };
        Closed += (_, _) =>
        {
            _statusTimer.Stop();
            _capture.Dispose();
            _overlay?.Close();
        };
    }

    private void OpacityChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (OpacityText is null) return;
        OpacityText.Text = $"{Math.Round(e.NewValue * 100)}%";
        _overlay?.SetShade(e.NewValue);
    }

    private void OverlaySizeChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (SizeText is null || HeightRatioSlider is null) return;
        SizeText.Text = $"{Math.Round(e.NewValue)} px";
        _overlay?.SetSize(e.NewValue, HeightRatioSlider.Value);
    }

    private void HeightRatioChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (HeightRatioText is null || SizeSlider is null) return;
        HeightRatioText.Text = $"{Math.Round(e.NewValue * 100)}%";
        _overlay?.SetSize(SizeSlider.Value, e.NewValue);
    }

    private void AlignToMinimap(object sender, RoutedEventArgs e) => ApplyMinimapPreset();

    private void ApplyMinimapPreset()
    {
        var area = SystemParameters.WorkArea;
        var width = Math.Clamp(Math.Round(area.Width * 0.19), 140, 500);
        const double heightRatio = 0.72;
        SizeSlider.Value = width;
        HeightRatioSlider.Value = heightRatio;
        if (_overlay is null) return;
        _overlay.SetSize(width, heightRatio);
        _overlay.Left = area.Right - width - 20;
        _overlay.Top = area.Bottom - width * heightRatio - 35;
    }

    private void ToggleLock(object sender, RoutedEventArgs e)
    {
        _locked = !_locked;
        _overlay?.SetLocked(_locked);
        LockButton.Content = _locked ? "Destravar posição" : "Travar posição";
    }

    private void ToggleCapture(object sender, RoutedEventArgs e)
    {
        if (_capture.IsRunning)
        {
            _capture.Stop();
            CaptureButton.Content = "Iniciar captura";
            UpdateCaptureStatus();
            return;
        }

        try
        {
            _capture.Start();
            CaptureButton.Content = "Parar captura";
        }
        catch (Exception error)
        {
            CaptureStatus.Text = $"Falha no Npcap: {error.Message}";
            CaptureButton.Content = "Tentar novamente";
        }
    }

    private void UpdateCaptureStatus()
    {
        var world = _capture.WorldSnapshot(ResourceRadiusSlider.Value, ResourceRotationSlider.Value);
        _overlay?.SetResources(ResourcePreviewCheck.IsChecked == true && _capture.IsRunning
            ? world.VisibleResources : Array.Empty<ResourcePoint>());
        _overlay?.SetPlayerAlert(PlayerAlertCheck.IsChecked == true && _capture.IsRunning &&
            world.DetectedPlayers > 0);
        if (_capture.LastError is { } error)
        {
            CaptureStatus.Text = $"Falha na captura: {error}";
            CaptureButton.Content = "Tentar novamente";
        }
        else if (_capture.IsRunning)
        {
            CaptureStatus.Text = $"{_capture.AdapterCount} adaptador(es) · {_capture.UdpPackets} UDP candidatos · {_capture.LikelyPhotonPackets} Photon prováveis\n" +
                $"Eventos: {_capture.DecodedEvents} · entradas de jogadores: {_capture.PlayerSpawns} · recursos: {_capture.ResourceSpawns} · movimentos: {_capture.MoveEvents} · erros: {_capture.DecodeErrors}\n" +
                $"Posição local: {(world.HasLocalPosition ? "recebida" : "aguardando movimento/entrada")}; recursos rastreados: {world.TrackedResources}; visíveis no raio: {world.VisibleResources.Count}; jogadores detectados: {world.DetectedPlayers}";
        }
        else if (CaptureButton.Content?.ToString() == "Iniciar captura")
        {
            CaptureStatus.Text = "Captura parada.";
        }
    }
}
