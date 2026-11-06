using System.Buffers.Binary;
using System.Net.NetworkInformation;
using System.Threading;
using AlbionPacketExplorer.PhotonWire;
using Libpcap;

namespace Jogo4592Radar;

/// <summary>
/// Passive capture diagnostics only. Packet contents are never stored or drawn as player markers.
/// </summary>
internal sealed class NpcapMonitor : IDisposable
{
    private const string Filter = "udp and (port 5055 or port 5056 or port 5058)";
    private PcapDispatcher? _dispatcher;
    private Thread? _worker;
    private volatile bool _running;
    private long _udpPackets;
    private long _likelyPhotonPackets;
    private long _decodedEvents;
    private long _playerSpawns;
    private long _resourceSpawns;
    private long _moveEvents;
    private long _decodeErrors;
    private long _moveRequests;
    private string? _lastError;
    private readonly PhotonPacketReader _photonReader = new();
    private readonly WorldStateTracker _world = new();

    public NpcapMonitor()
    {
        _photonReader.OnEvent += OnPhotonEvent;
        _photonReader.OnRequest += OnPhotonRequest;
        _photonReader.OnResponse += _world.ObserveResponse;
    }

    public int AdapterCount { get; private set; }
    public bool IsRunning => _running;
    public long UdpPackets => Interlocked.Read(ref _udpPackets);
    public long LikelyPhotonPackets => Interlocked.Read(ref _likelyPhotonPackets);
    public long DecodedEvents => Interlocked.Read(ref _decodedEvents);
    public long PlayerSpawns => Interlocked.Read(ref _playerSpawns);
    public long ResourceSpawns => Interlocked.Read(ref _resourceSpawns);
    public long MoveEvents => Interlocked.Read(ref _moveEvents);
    public long DecodeErrors => Interlocked.Read(ref _decodeErrors);
    public long MoveRequests => Interlocked.Read(ref _moveRequests);
    public WorldSnapshot WorldSnapshot(double radius, double rotationDegrees) => _world.Snapshot(radius, rotationDegrees);
    public string? LastError => _lastError;

    public int Start()
    {
        if (_running) return AdapterCount;
        if (_dispatcher is not null) Stop();
        _lastError = null;
        Interlocked.Exchange(ref _udpPackets, 0);
        Interlocked.Exchange(ref _likelyPhotonPackets, 0);
        Interlocked.Exchange(ref _decodedEvents, 0);
        Interlocked.Exchange(ref _playerSpawns, 0);
        Interlocked.Exchange(ref _resourceSpawns, 0);
        Interlocked.Exchange(ref _moveEvents, 0);
        Interlocked.Exchange(ref _decodeErrors, 0);
        Interlocked.Exchange(ref _moveRequests, 0);
        _world.Reset();

        var dispatcher = new PcapDispatcher(OnPacket);
        var opened = 0;
        try
        {
            var candidates = Pcap.ListDevices()
                .Where(device => !device.Flags.HasFlag(PcapDeviceFlags.Loopback) &&
                                 device.Flags.HasFlag(PcapDeviceFlags.Up)).ToArray();
            var preferredIds = PreferredAdapterIds();
            var preferred = candidates.Where(device => preferredIds.Any(id =>
                device.Name.Contains(id, StringComparison.OrdinalIgnoreCase))).ToArray();
            if (preferred.Length > 0) candidates = preferred;

            foreach (var device in candidates)
            {
                try
                {
                    dispatcher.OpenDevice(device, pcap => pcap.NonBlocking = true);
                    dispatcher.Filter = Filter;
                    opened++;
                }
                catch (PcapException)
                {
                    // Another active adapter may still be usable.
                }
            }

            if (opened == 0) throw new InvalidOperationException("Nenhum adaptador Npcap ativo pôde ser aberto.");
            _dispatcher = dispatcher;
            AdapterCount = opened;
            _running = true;
            _worker = new Thread(CaptureLoop) { IsBackground = true, Name = "Npcap diagnostics" };
            _worker.Start();
            return opened;
        }
        catch
        {
            dispatcher.Dispose();
            throw;
        }
    }

    public void Stop()
    {
        _running = false;
        if (_worker?.IsAlive == true) _worker.Join(TimeSpan.FromSeconds(2));
        _worker = null;
        _dispatcher?.Dispose();
        _dispatcher = null;
        AdapterCount = 0;
    }

    private void CaptureLoop()
    {
        try
        {
            while (_running)
            {
                var count = _dispatcher?.Dispatch(50) ?? 0;
                if (count == 0) Thread.Sleep(10);
            }
        }
        catch (Exception error)
        {
            _lastError = error.Message;
            _running = false;
        }
    }

    private void OnPacket(Pcap pcap, ref Packet packet)
    {
        if (packet.CapturedLength < packet.DeclaredLength) return;
        if (!TryGetUdpPayload(pcap.DataLink, packet.Data, out var payload)) return;
        Interlocked.Increment(ref _udpPackets);
        if (!LooksLikePhoton(payload)) return;
        Interlocked.Increment(ref _likelyPhotonPackets);
        try { _photonReader.ReadPacket(payload.ToArray()); }
        catch { Interlocked.Increment(ref _decodeErrors); }
    }

    private void OnPhotonEvent(PhotonEvent message)
    {
        Interlocked.Increment(ref _decodedEvents);
        var code = (int)message.Code;
        if (message.Parameters.TryGetValue(252, out var rawCode))
        {
            try { code = Convert.ToInt32(rawCode); }
            catch { /* Keep the fallback event code. */ }
        }

        if (code == 29) Interlocked.Increment(ref _playerSpawns);
        else if (code is 38 or 39 or 40) Interlocked.Increment(ref _resourceSpawns);
        else if (code == 3) Interlocked.Increment(ref _moveEvents);
        _world.ObserveEvent(message);
    }

    private void OnPhotonRequest(PhotonRequest request)
    {
        var code = (int)request.OperationCode;
        if (request.Parameters.TryGetValue(253, out var rawCode))
        {
            try { code = Convert.ToInt32(rawCode); }
            catch { /* Keep the fallback operation code. */ }
        }
        if (code is 21 or 22) Interlocked.Increment(ref _moveRequests);
        _world.ObserveRequest(request);
    }

    private static bool TryGetUdpPayload(PcapDataLink link, ReadOnlySpan<byte> frame, out ReadOnlySpan<byte> payload)
    {
        payload = default;
        ReadOnlySpan<byte> ip;
        if (link == PcapDataLink.DLT_EN10MB)
        {
            if (frame.Length < 14) return false;
            var etherType = BinaryPrimitives.ReadUInt16BigEndian(frame.Slice(12, 2));
            var offset = 14;
            if (etherType is 0x8100 or 0x88A8)
            {
                if (frame.Length < 18) return false;
                etherType = BinaryPrimitives.ReadUInt16BigEndian(frame.Slice(16, 2));
                offset = 18;
            }
            if (etherType is not (0x0800 or 0x86DD)) return false;
            ip = frame[offset..];
        }
        else if (link == PcapDataLink.DLT_RAW) ip = frame;
        else return false;

        ReadOnlySpan<byte> udp;
        if (ip.Length < 1) return false;
        switch (ip[0] >> 4)
        {
            case 4:
                if (ip.Length < 20 || ip[9] != 17) return false;
                var headerLength = (ip[0] & 0x0F) * 4;
                if (headerLength < 20 || ip.Length < headerLength + 8) return false;
                // Fragmented IPv4 datagrams are deliberately not decoded by this diagnostic.
                if ((BinaryPrimitives.ReadUInt16BigEndian(ip.Slice(6, 2)) & 0x3FFF) != 0) return false;
                udp = ip[headerLength..];
                break;
            case 6:
                if (ip.Length < 48 || ip[6] != 17) return false;
                udp = ip[40..];
                break;
            default: return false;
        }

        if (udp.Length < 8) return false;
        var udpLength = BinaryPrimitives.ReadUInt16BigEndian(udp.Slice(4, 2));
        if (udpLength < 8 || udpLength > udp.Length) return false;
        var sourcePort = BinaryPrimitives.ReadUInt16BigEndian(udp.Slice(0, 2));
        var destinationPort = BinaryPrimitives.ReadUInt16BigEndian(udp.Slice(2, 2));
        if (!IsPhotonPort(sourcePort) && !IsPhotonPort(destinationPort)) return false;
        payload = udp.Slice(8, udpLength - 8);
        return payload.Length > 0;
    }

    private static bool IsPhotonPort(ushort port) => port is 5055 or 5056 or 5058;

    private static bool LooksLikePhoton(ReadOnlySpan<byte> payload) =>
        payload.Length >= 12 && payload[2] is 0 or 1 or 0xCC && payload[3] <= 32;

    public void Dispose() => Stop();

    private static HashSet<string> PreferredAdapterIds()
    {
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var network in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (network.OperationalStatus != OperationalStatus.Up) continue;
            try
            {
                if (network.GetIPProperties().GatewayAddresses.Count > 0) ids.Add(network.Id);
            }
            catch (NetworkInformationException)
            {
                // The Npcap device list remains available as a fallback.
            }
        }
        return ids;
    }
}
