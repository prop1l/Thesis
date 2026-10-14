using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Thesis.Algorithms;

namespace Thesis.Controls;

/// <summary>
/// Линейный график сложности. Рисуется векторно через DrawingContext,
/// цвета берутся из ресурсов темы, поэтому график перекрашивается вместе с приложением.
/// </summary>
public class PerformanceChart : Control
{
    public static readonly StyledProperty<IReadOnlyList<PerformancePoint>?> DataProperty =
        AvaloniaProperty.Register<PerformanceChart, IReadOnlyList<PerformancePoint>?>(nameof(Data));

    public IReadOnlyList<PerformancePoint>? Data
    {
        get => GetValue(DataProperty);
        set => SetValue(DataProperty, value);
    }

    private static readonly string[] SeriesBrushKeys = ["ChartSeries1Brush", "ChartSeries2Brush", "ChartSeries3Brush"];
    private static readonly Typeface Font = new(FontFamily.Default);

    // Отступы области построения: слева подписи оси Y, снизу — оси X, сверху — легенда
    private const double Left = 44, Right = 12, Top = 30, Bottom = 26;

    static PerformanceChart()
    {
        AffectsRender<PerformanceChart>(DataProperty);
    }

    public PerformanceChart()
    {
        ActualThemeVariantChanged += (_, _) => InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        var text = Brush("TextSecondaryBrush");
        var grid = Brush("ChartGridBrush");
        var data = Data ?? [];

        var plot = new Rect(Left, Top, Math.Max(0, Bounds.Width - Left - Right), Math.Max(0, Bounds.Height - Top - Bottom));
        if (plot.Width < 10 || plot.Height < 10)
            return;

        if (data.Count == 0)
        {
            DrawText(context, "Нажмите «Построить график»", plot.Center, text, 13, center: true);
            return;
        }

        var series = data
            .Where(p => p.TheoreticalTime > 0)
            .GroupBy(p => p.Complexity)
            .Select(g => g.OrderBy(p => p.VertexCount).ToList())
            .ToList();

        var maxX = Math.Max(1, data.Max(p => p.VertexCount));
        var maxY = Math.Max(1, data.Max(p => p.TheoreticalTime));

        // Сетка и подписи оси Y
        var gridPen = new Pen(grid, 1);
        for (var i = 0; i <= 4; i++)
        {
            var y = plot.Bottom - plot.Height * i / 4;
            context.DrawLine(gridPen, new Point(plot.Left, y), new Point(plot.Right, y));
            DrawText(context, (maxY * i / 4).ToString("0", CultureInfo.CurrentCulture), new Point(plot.Left - 6, y), text, 10, alignRight: true);
        }

        // Подписи оси X; слишком близкие к предыдущей пропускаем, чтобы не налезали
        var lastLabelX = double.NegativeInfinity;
        foreach (var x in data.Select(p => p.VertexCount).Distinct().OrderBy(x => x))
        {
            var px = plot.Left + plot.Width * x / maxX;
            if (px - lastLabelX < 26)
                continue;

            DrawText(context, x.ToString(CultureInfo.CurrentCulture), new Point(px, plot.Bottom + 12), text, 10, center: true);
            lastLabelX = px;
        }

        // Линии серий и легенда
        var legendX = plot.Left;
        for (var s = 0; s < series.Count; s++)
        {
            var brush = Brush(SeriesBrushKeys[s % SeriesBrushKeys.Length]);
            var pen = new Pen(brush, 2.2, lineCap: PenLineCap.Round, lineJoin: PenLineJoin.Round);
            var points = series[s]
                .Select(p => new Point(plot.Left + plot.Width * p.VertexCount / maxX, plot.Bottom - plot.Height * p.TheoreticalTime / maxY))
                .ToList();

            for (var i = 1; i < points.Count; i++)
                context.DrawLine(pen, points[i - 1], points[i]);

            foreach (var point in points)
                context.DrawEllipse(brush, null, point, 2.5, 2.5);

            context.DrawEllipse(brush, null, new Point(legendX + 5, 10), 4, 4);
            var label = Format(series[s][0].Complexity, text, 11);
            context.DrawText(label, new Point(legendX + 13, 10 - label.Height / 2));
            legendX += label.Width + 30;
        }
    }

    private IBrush Brush(string key) =>
        this.TryFindResource(key, ActualThemeVariant, out var value) && value is IBrush brush ? brush : Brushes.Gray;

    private static FormattedText Format(string text, IBrush brush, double size) =>
        new(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, Font, size, brush);

    private static void DrawText(DrawingContext context, string value, Point anchor, IBrush brush, double size,
        bool center = false, bool alignRight = false)
    {
        var text = Format(value, brush, size);
        var x = center ? anchor.X - text.Width / 2 : alignRight ? anchor.X - text.Width : anchor.X;
        context.DrawText(text, new Point(x, anchor.Y - text.Height / 2));
    }
}
