using System.Globalization;
using AlbionPacketExplorer.PhotonWire;

namespace Jogo4592Radar;

internal readonly record struct ResourcePoint(double X, double Y);
internal readonly record struct WorldSnapshot(bool HasLocalPosition, int TrackedResources, IReadOnlyList<ResourcePoint> VisibleResources);

/// <summary>Tracks only positions that appear explicitly in decoded wire events.</summary>
internal sealed class WorldStateTracker
{
    private readonly object _gate = new();
    private readonly Dictionary<long, ResourceEntity> _resources = new();
    private double? _localX;
    private double? _localY;

    private readonly record struct ResourceEntity(double X, double Y, DateTime LastSeen);

    public void Reset()
    {
        lock (_gate)
        {
            _resources.Clear();
            _localX = _localY = null;
        }
    }

    public void ObserveRequest(PhotonRequest request)
    {
        if (WireCode(request.Parameters, 253, request.OperationCode) is not (21 or 22) ||
            !request.Parameters.TryGetValue(1, out var rawPosition) ||
            !TryPosition(rawPosition, out var x, out var y)) return;

        lock (_gate) SetLocalPosition(x, y);
    }

    public void ObserveResponse(PhotonResponse response)
    {
        if (WireCode(response.Parameters, 253, response.OperationCode) != 2 ||
            !response.Parameters.TryGetValue(9, out var rawPosition) ||
            !TryPosition(rawPosition, out var x, out var y)) return;

        lock (_gate)
        {
            _resources.Clear();
            _localX = x;
            _localY = y;
        }
    }

    public void ObserveEvent(PhotonEvent message)
    {
        var code = WireCode(message.Parameters, 252, message.Code);
        if (code is not (1 or 39 or 40)) return;
        var parameters = message.Parameters;
        lock (_gate)
        {
            if (code == 1 && TryId(parameters.GetValueOrDefault((byte)0), out var leaveId))
            {
                _resources.Remove(leaveId);
            }
            else if (code == 40 &&
                     TryId(parameters.GetValueOrDefault((byte)0), out var id) &&
                     TryPosition(parameters.GetValueOrDefault((byte)8), out var x, out var y))
            {
                AddResource(id, x, y);
            }
            else if (code == 39 &&
                     parameters.GetValueOrDefault((byte)0) is Array ids &&
                     parameters.GetValueOrDefault((byte)3) is Array coordinates)
            {
                var count = Math.Min(ids.Length, coordinates.Length / 2);
                for (var i = 0; i < count; i++)
                {
                    if (!TryId(ids.GetValue(i), out var batchId) ||
                        !TryNumber(coordinates.GetValue(i * 2), out var batchX) ||
                        !TryNumber(coordinates.GetValue(i * 2 + 1), out var batchY)) continue;
                    AddResource(batchId, batchX, batchY);
                }
            }
        }
    }

    public WorldSnapshot Snapshot(double radius, double rotationDegrees)
    {
        radius = Math.Clamp(radius, 10, 300);
        var radians = rotationDegrees * Math.PI / 180;
        var cos = Math.Cos(radians);
        var sin = Math.Sin(radians);
        lock (_gate)
        {
            var expired = _resources.Where(item => DateTime.UtcNow - item.Value.LastSeen > TimeSpan.FromSeconds(60))
                .Select(item => item.Key).ToArray();
            foreach (var id in expired) _resources.Remove(id);

            if (_localX is not { } localX || _localY is not { } localY)
                return new WorldSnapshot(false, _resources.Count, Array.Empty<ResourcePoint>());

            var visible = new List<ResourcePoint>();
            foreach (var resource in _resources.Values)
            {
                var dx = resource.X - localX;
                var dy = resource.Y - localY;
                if (dx * dx + dy * dy > radius * radius) continue;
                var rotatedX = dx * cos - dy * sin;
                var rotatedY = dx * sin + dy * cos;
                visible.Add(new ResourcePoint(0.5 + rotatedX / (2 * radius), 0.5 + rotatedY / (2 * radius)));
            }
            return new WorldSnapshot(true, _resources.Count, visible);
        }
    }

    private void SetLocalPosition(double x, double y)
    {
        if (_localX is { } oldX && _localY is { } oldY &&
            Math.Pow(x - oldX, 2) + Math.Pow(y - oldY, 2) > 150 * 150)
            _resources.Clear();
        _localX = x;
        _localY = y;
    }

    private void AddResource(long id, double x, double y)
    {
        if (_resources.Count >= 1000 && !_resources.ContainsKey(id))
        {
            var oldest = _resources.MinBy(item => item.Value.LastSeen).Key;
            _resources.Remove(oldest);
        }
        _resources[id] = new ResourceEntity(x, y, DateTime.UtcNow);
    }

    private static int WireCode(IReadOnlyDictionary<byte, object?> parameters, byte key, byte fallback)
    {
        if (parameters.TryGetValue(key, out var value) && TryNumber(value, out var code))
            return (int)code;
        return fallback;
    }

    private static bool TryPosition(object? value, out double x, out double y)
    {
        x = y = 0;
        if (value is byte[] bytes && bytes.Length >= 8)
        {
            x = BitConverter.ToSingle(bytes, 0);
            y = BitConverter.ToSingle(bytes, 4);
            return double.IsFinite(x) && double.IsFinite(y);
        }
        if (value is not Array coordinates || coordinates.Length < 2 ||
            !TryNumber(coordinates.GetValue(0), out x) ||
            !TryNumber(coordinates.GetValue(1), out y)) return false;
        return true;
    }

    private static bool TryId(object? value, out long id)
    {
        id = 0;
        if (!TryNumber(value, out var number) || number is < long.MinValue or > long.MaxValue) return false;
        id = (long)number;
        return true;
    }

    private static bool TryNumber(object? value, out double number)
    {
        number = 0;
        try
        {
            if (value is null) return false;
            number = Convert.ToDouble(value, CultureInfo.InvariantCulture);
            return double.IsFinite(number);
        }
        catch { return false; }
    }
}
