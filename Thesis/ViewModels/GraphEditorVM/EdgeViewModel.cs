using Avalonia;
using Avalonia.Collections;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Thesis.Helpers;
using Thesis.Models;

namespace Thesis.ViewModels;

/// <summary>
/// Ребро на холсте. Считает геометрию (линия, стрелка, позиция веса) и внешний вид,
/// который раньше вычислялся вручную в code-behind окна.
/// </summary>
public partial class EdgeViewModel : ObservableObject
{
    private static readonly AvaloniaList<double> DashPattern = [4, 4];

    private readonly Edge _edge;
    private readonly GraphStyle _graphStyle;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Stroke), nameof(StrokeThickness))]
    private bool isExamined;

    [ObservableProperty]
    private bool isHighlighted;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Stroke), nameof(StrokeThickness))]
    private bool isPathEdge;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Stroke), nameof(StrokeThickness))]
    private bool isSelected;

    public EdgeViewModel(Edge edge, NodeViewModel source, NodeViewModel target, GraphStyle graphStyle)
    {
        _edge = edge;
        Source = source;
        Target = target;
        _graphStyle = graphStyle;

        Source.PropertyChanged += OnNodeChanged;
        Target.PropertyChanged += OnNodeChanged;
        _graphStyle.Edge.PropertyChanged += OnEdgeStyleChanged;
    }

    public NodeViewModel Source { get; }
    public NodeViewModel Target { get; }

    public string Id => _edge.Id;
    public string SourceId => _edge.SourceId;
    public string TargetId => _edge.TargetId;

    public string Label
    {
        get => _edge.Label;
        set => SetProperty(_edge.Label, value, _edge, (m, v) => m.Label = v);
    }

    public decimal? Weight
    {
        get => _edge.Weight;
        set
        {
            if (SetProperty(_edge.Weight, value, _edge, (m, v) => m.Weight = v))
                OnPropertyChanged(nameof(WeightText));
        }
    }

    public Edge GetEdge() => _edge;

    #region Geometry

    // Линия идёт не от центров, а от границ эллипсов
    public Point StartPoint => GetBorderPoint(Source.Center, Target.Center, Source);
    public Point EndPoint => GetBorderPoint(Target.Center, Source.Center, Target);
    public Point MidPoint => new((StartPoint.X + EndPoint.X) / 2, (StartPoint.Y + EndPoint.Y) / 2);

    public double WeightLeft => MidPoint.X - 18;
    public double WeightTop => MidPoint.Y - 12;

    /// <summary>Наконечник стрелки: левый «ус», остриё, правый «ус». Пусто, если ребро нулевой длины.</summary>
    public IList<Point> ArrowPoints => GraphGeometryHelper.GetArrowPoints(StartPoint, EndPoint);

    private static Point GetBorderPoint(Point center, Point otherCenter, NodeViewModel node)
    {
        var dx = otherCenter.X - center.X;
        var dy = otherCenter.Y - center.Y;

        if (Math.Abs(dx) < 0.001 && Math.Abs(dy) < 0.001)
            return center;

        var rx = node.Width / 2.0;
        var ry = node.Height / 2.0;

        var scale = 1.0 / Math.Sqrt((dx * dx) / (rx * rx) + (dy * dy) / (ry * ry));

        return new Point(center.X + dx * scale, center.Y + dy * scale);
    }

    #endregion

    #region Appearance

    public IBrush Stroke =>
        IsPathEdge ? Brushes.SeaGreen
        : IsExamined ? Brushes.Orange
        : IsSelected ? Brushes.DodgerBlue
        : BrushHelper.CreateBrush(_graphStyle.Edge.Stroke);

    public double StrokeThickness =>
        _graphStyle.Edge.StrokeThickness +
        (IsPathEdge ? 3 : IsExamined ? 2 : IsSelected ? 1 : 0);

    public AvaloniaList<double>? StrokeDashArray => _graphStyle.Edge.IsDashed ? DashPattern : null;

    public double FontSize => _graphStyle.Edge.FontSize;

    public string WeightText => Weight?.ToString() ?? "0";

    #endregion

    /// <summary>Отписка от вершин и стиля, когда ребро удаляется из графа.</summary>
    public void Detach()
    {
        Source.PropertyChanged -= OnNodeChanged;
        Target.PropertyChanged -= OnNodeChanged;
        _graphStyle.Edge.PropertyChanged -= OnEdgeStyleChanged;
    }

    private void OnNodeChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(NodeViewModel.Center))
            NotifyGeometryChanged();
    }

    private void OnEdgeStyleChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(EdgeStyle.Stroke):
                OnPropertyChanged(nameof(Stroke));
                break;
            case nameof(EdgeStyle.StrokeThickness):
                OnPropertyChanged(nameof(StrokeThickness));
                break;
            case nameof(EdgeStyle.IsDashed):
                OnPropertyChanged(nameof(StrokeDashArray));
                break;
            case nameof(EdgeStyle.FontSize):
                OnPropertyChanged(nameof(FontSize));
                break;
        }
    }

    private void NotifyGeometryChanged()
    {
        OnPropertyChanged(nameof(StartPoint));
        OnPropertyChanged(nameof(EndPoint));
        OnPropertyChanged(nameof(MidPoint));
        OnPropertyChanged(nameof(WeightLeft));
        OnPropertyChanged(nameof(WeightTop));
        OnPropertyChanged(nameof(ArrowPoints));
    }
}
