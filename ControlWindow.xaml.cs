using System;
using System.Windows;
using System.Windows.Threading;

namespace Jogo4592Radar;

public partial class ControlWindow : Window
{
    private OverlayWindow? _overlay;
    private bool _running = true;
    private bool _locked;
    private readonly NpcapMonitor _capture = new();
    private readonly DispatcherTimer _statusTimer = new() { Interval = TimeSpan.FromMilliseconds(500) };

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
            _overlay = new OverlayWindow
            {
                Left = SystemParameters.WorkArea.Right - 270,
                Top = SystemParameters.WorkArea.Bottom - 270
            };
            _overlay.SetSize(SizeSlider.Value);
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
        if (_capture.LastError is { } error)
        {
            CaptureStatus.Text = $"Falha na captura: {error}";
            CaptureButton.Content = "Tentar novamente";
        }
        else if (_capture.IsRunning)
        {
            CaptureStatus.Text = $"{_capture.AdapterCount} adaptador(es) · {_capture.UdpPackets} UDP candidatos · {_capture.LikelyPhotonPackets} Photon prováveis\n" +
                $"Eventos decodificados: {_capture.DecodedEvents} · entrada de jogador: {_capture.PlayerSpawns} · recursos: {_capture.ResourceSpawns} · movimento: {_capture.MoveEvents} · erros: {_capture.DecodeErrors}";
        }
        else if (CaptureButton.Content?.ToString() == "Iniciar captura")
        {
            CaptureStatus.Text = "Captura parada.";
        }
    }
}
