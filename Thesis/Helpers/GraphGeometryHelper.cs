using Avalonia;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using System;

namespace Thesis.Helpers;

public static class GraphGeometryHelper
{
    public static Point GetNodeCenter(double x, double y, double width, double height)
    {
        return new Point(x + width / 2, y + height / 2);
    }

    public static double Distance(Point p1, Point p2)
    {
        var dx = p1.X - p2.X;
        var dy = p1.Y - p2.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    public static double DistanceToSegment(Point p, Point a, Point b)
    {
        var dx = b.X - a.X;
        var dy = b.Y - a.Y;

        if (dx == 0 && dy == 0)
            return Distance(p, a);

        var t = ((p.X - a.X) * dx + (p.Y - a.Y) * dy) / (dx * dx + dy * dy);
        t = Math.Clamp(t, 0, 1);

        var nearest = new Point(a.X + t * dx, a.Y + t * dy);
        return Distance(p, nearest);
    }

    public static void UpdateArrow(
        Line mainLine,
        Line arrowLeft,
        Line arrowRight,
        bool isDirected,
        IBrush stroke,
        double thickness)
    {
        if (!isDirected)
        {
            arrowLeft.IsVisible = false;
            arrowRight.IsVisible = false;
            return;
        }

        var start = mainLine.StartPoint;
        var end = mainLine.EndPoint;

        var dx = end.X - start.X;
        var dy = end.Y - start.Y;
        var length = Math.Sqrt(dx * dx + dy * dy);

        if (length < 0.001)
        {
            arrowLeft.IsVisible = false;
            arrowRight.IsVisible = false;
            return;
        }

        arrowLeft.IsVisible = true;
        arrowRight.IsVisible = true;

        var ux = dx / length;
        var uy = dy / length;

        const double arrowLength = 14;
        const double arrowWidth = 6;
        const double endOffset = 2;

        var tip = new Point(
            end.X - ux * endOffset,
            end.Y - uy * endOffset);

        var baseX = tip.X - ux * arrowLength;
        var baseY = tip.Y - uy * arrowLength;

        var perpX = -uy;
        var perpY = ux;

        arrowLeft.StartPoint = tip;
        arrowLeft.EndPoint = new Point(baseX + perpX * arrowWidth, baseY + perpY * arrowWidth);

        arrowRight.StartPoint = tip;
        arrowRight.EndPoint = new Point(baseX - perpX * arrowWidth, baseY - perpY * arrowWidth);

        arrowLeft.Stroke = stroke;
        arrowRight.Stroke = stroke;
        arrowLeft.StrokeThickness = thickness;
        arrowRight.StrokeThickness = thickness;
        arrowLeft.StrokeDashArray = null;
        arrowRight.StrokeDashArray = null;
    }
}