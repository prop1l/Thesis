using Avalonia;
using System;
using System.Collections.Generic;

namespace Thesis.Helpers;

public static class GraphGeometryHelper
{
    private const double ArrowLength = 14;
    private const double ArrowWidth = 6;
    private const double ArrowEndOffset = 2;

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

    /// <summary>
    /// Точки наконечника стрелки на конце отрезка: [левый ус, остриё, правый ус].
    /// Для отрезка нулевой длины возвращает пустой список.
    /// </summary>
    public static IList<Point> GetArrowPoints(Point start, Point end)
    {
        var dx = end.X - start.X;
        var dy = end.Y - start.Y;
        var length = Math.Sqrt(dx * dx + dy * dy);

        if (length < 0.001)
            return [];

        var ux = dx / length;
        var uy = dy / length;

        var tip = new Point(end.X - ux * ArrowEndOffset, end.Y - uy * ArrowEndOffset);

        var baseX = tip.X - ux * ArrowLength;
        var baseY = tip.Y - uy * ArrowLength;

        // Перпендикуляр к направлению ребра
        var perpX = -uy;
        var perpY = ux;

        return
        [
            new Point(baseX + perpX * ArrowWidth, baseY + perpY * ArrowWidth),
            tip,
            new Point(baseX - perpX * ArrowWidth, baseY - perpY * ArrowWidth)
        ];
    }
}
