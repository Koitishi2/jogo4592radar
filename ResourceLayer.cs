using System.Windows;
using System.Windows.Media;

namespace Jogo4592Radar;

/// <summary>Draws all resources in one WPF visual, without a UI element per marker.</summary>
public sealed class ResourceLayer : FrameworkElement
{
    private static readonly Brush MarkerBrush = FrozenBrush(Colors.LimeGreen);
    private static readonly Pen MarkerOutline = FrozenPen();
    private IReadOnlyList<ResourcePoint> _points = Array.Empty<ResourcePoint>();

    internal void SetPoints(IReadOnlyList<ResourcePoint> points)
    {
        if (SamePoints(_points, points)) return;
        _points = points;
        InvalidateVisual();
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        var width = RenderSize.Width;
        var height = RenderSize.Height;
        if (width <= 0 || height <= 0) return;

        foreach (var point in _points)
            drawingContext.DrawEllipse(MarkerBrush, MarkerOutline,
                new Point(point.X * width, point.Y * height), 4.5, 4.5);
    }

    private static bool SamePoints(IReadOnlyList<ResourcePoint> left, IReadOnlyList<ResourcePoint> right)
    {
        if (left.Count != right.Count) return false;
        for (var i = 0; i < left.Count; i++)
            if (left[i] != right[i]) return false;
        return true;
    }

    private static Brush FrozenBrush(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }

    private static Pen FrozenPen()
    {
        var pen = new Pen(Brushes.Black, 1);
        pen.Freeze();
        return pen;
    }
}
