using Avalonia.Controls;
using Avalonia.Media.Imaging;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using Thesis.Models;

namespace Thesis.Controls;

public partial class PerformanceChart : UserControl
{
    private List<PerformancePoint> _data = new();

    public PerformanceChart()
    {
        InitializeComponent();

        this.PropertyChanged += (s, e) =>
        {
            if (e.Property == BoundsProperty && _data.Any())
                DrawChart();
        };
    }

    // ПУБЛИЧНЫЙ МЕТОД ДЛЯ УСТАНОВКИ ДАННЫХ
    public void SetData(List<PerformancePoint> data)
    {
        _data = data ?? new List<PerformancePoint>();
        DrawChart();
    }

    private void DrawChart()
    {
        if (_data == null || !_data.Any())
        {
            NoDataText.IsVisible = true;
            ChartImage.Source = null;
            return;
        }

        NoDataText.IsVisible = false;

        var width = (int)Bounds.Width;
        var height = (int)Bounds.Height;

        if (width <= 0 || height <= 0)
        {
            width = 500;
            height = 250;
        }

        using var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);

        canvas.Clear(SKColors.White);

        var margin = new SKRect(60, 40, 40, 50);
        var chartRect = new SKRect(
            margin.Left,
            margin.Top,
            width - margin.Right,
            height - margin.Bottom
        );

        DrawAxes(canvas, chartRect);
        DrawData(canvas, chartRect);
        DrawLabels(canvas, chartRect);

        using var image = SKImage.FromBitmap(bitmap);
        var data = image.Encode(SKEncodedImageFormat.Png, 100);

        using var stream = new System.IO.MemoryStream(data.ToArray());
        var avaloniaImage = new Bitmap(stream);

        ChartImage.Source = avaloniaImage;
    }

    private void DrawAxes(SKCanvas canvas, SKRect chartRect)
    {
        using var paint = new SKPaint
        {
            Color = SKColors.Black,
            StrokeWidth = 2,
            IsAntialias = true
        };

        canvas.DrawLine(chartRect.Left, chartRect.Bottom, chartRect.Right, chartRect.Bottom, paint);
        canvas.DrawLine(chartRect.Left, chartRect.Top, chartRect.Left, chartRect.Bottom, paint);
    }

    private void DrawData(SKCanvas canvas, SKRect chartRect)
    {
        if (!_data.Any()) return;

        var groups = _data
            .Where(p => p.TheoreticalTime > 0)
            .GroupBy(p => p.Complexity)
            .ToDictionary(g => g.Key, g => g.OrderBy(p => p.VertexCount).ToList());

        var colors = new Dictionary<string, SKColor>
        {
            ["O(1)"] = new SKColor(0, 0, 255),
            ["O(log V)"] = new SKColor(0, 200, 0),
            ["O(V)"] = new SKColor(255, 200, 0),
            ["O(V log V)"] = new SKColor(255, 140, 0),
            ["O(V²)"] = new SKColor(255, 0, 0),
            ["O(V³)"] = new SKColor(128, 0, 128),
            ["Сравнение"] = new SKColor(0, 200, 200)
        };

        var allTimes = _data.Where(p => p.TheoreticalTime > 0).Select(p => p.TheoreticalTime).ToList();
        var maxTime = allTimes.Any() ? (float)allTimes.Max() : 1f;
        if (maxTime < 1) maxTime = 1;

        foreach (var group in groups)
        {
            var color = colors.ContainsKey(group.Key) ? colors[group.Key] : SKColors.Gray;
            var points = new List<SKPoint>();

            foreach (var item in group.Value)
            {
                var x = chartRect.Left + (item.VertexCount / 100f) * chartRect.Width;
                var y = chartRect.Bottom - (float)(item.TheoreticalTime / maxTime) * chartRect.Height;
                points.Add(new SKPoint(Math.Min(x, chartRect.Right), Math.Max(y, chartRect.Top)));
            }

            if (points.Count >= 2)
            {
                using var paint = new SKPaint
                {
                    Color = color,
                    StrokeWidth = 3,
                    IsAntialias = true,
                    Style = SKPaintStyle.Stroke
                };

                canvas.DrawPoints(SKPointMode.Polygon, points.ToArray(), paint);

                using var pointPaint = new SKPaint
                {
                    Color = color,
                    Style = SKPaintStyle.Fill,
                    IsAntialias = true
                };

                foreach (var point in points)
                {
                    canvas.DrawCircle(point.X, point.Y, 5, pointPaint);
                }
            }
        }
    }

    private void DrawLabels(SKCanvas canvas, SKRect chartRect)
    {
        using var font = new SKFont { Size = 12 };
        using var paint = new SKPaint
        {
            Color = SKColors.Black,
            IsAntialias = true
        };

        font.Size = 14;

        canvas.DrawText(
            "Количество вершин (V)",
            chartRect.MidX,
            chartRect.Bottom + 30,
            SKTextAlign.Center,
            font,
            paint
        );

        canvas.DrawText(
            "Время (усл. ед.)",
            chartRect.Left - 50,
            chartRect.MidY,
            SKTextAlign.Center,
            font,
            paint
        );

        font.Size = 10;
        for (int i = 0; i <= 100; i += 20)
        {
            var x = chartRect.Left + (i / 100f) * chartRect.Width;
            canvas.DrawText(
                i.ToString(),
                x,
                chartRect.Bottom + 18,
                SKTextAlign.Center,
                font,
                paint
            );
        }

        var allTimes = _data.Where(p => p.TheoreticalTime > 0).Select(p => p.TheoreticalTime).ToList();
        var maxTime = allTimes.Any() ? (float)allTimes.Max() : 1f;
        if (maxTime < 1) maxTime = 1;

        for (int i = 0; i <= 4; i++)
        {
            var yPos = chartRect.Top + (i / 4f) * chartRect.Height;
            var value = maxTime * (1 - i / 4f);

            canvas.DrawText(
                value.ToString("F1"),
                chartRect.Left - 8,
                yPos + 4,
                SKTextAlign.Right,
                font,
                paint
            );
        }
    }
}