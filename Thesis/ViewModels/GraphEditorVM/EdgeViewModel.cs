using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Thesis.Models;

namespace Thesis.ViewModels;

public partial class EdgeViewModel : ObservableObject
{
    private readonly Edge _edge;
    private readonly ObservableCollection<Node> _nodes;
    private readonly GraphStyle _graphStyle;

    [ObservableProperty] private bool isExamined;
    [ObservableProperty] private bool isHighlighted;
    [ObservableProperty] private bool isPathEdge;

    public EdgeViewModel(Edge edge, ObservableCollection<Node> nodes, GraphStyle graphStyle)
    {
        _edge = edge;
        _nodes = nodes;
        _graphStyle = graphStyle;

        _edge.PropertyChanged += EdgeOnPropertyChanged;
        _nodes.CollectionChanged += NodesOnCollectionChanged;
        _graphStyle.Node.PropertyChanged += GraphNodeStyleOnPropertyChanged;

        SubscribeToNodes(_nodes);
    }

    public string Id => _edge.Id;

    public string SourceId
    {
        get => _edge.SourceId;
        set => _edge.SourceId = value;
    }

    public string TargetId
    {
        get => _edge.TargetId;
        set => _edge.TargetId = value;
    }

    public string Label
    {
        get => _edge.Label;
        set => SetProperty(_edge.Label, value, _edge, (m, v) => m.Label = v);
    }

    public decimal? Weight
    {
        get => _edge.Weight;
        set => SetProperty(_edge.Weight, value, _edge, (m, v) => m.Weight = v);
    }

    public Point RawStartPoint
    {
        get
        {
            var node = _nodes.FirstOrDefault(n => n.Id == SourceId);
            return node is null
                ? default
                : new Point(node.X + _graphStyle.Node.Width / 2, node.Y + _graphStyle.Node.Height / 2);
        }
    }

    public Point RawEndPoint
    {
        get
        {
            var node = _nodes.FirstOrDefault(n => n.Id == TargetId);
            return node is null
                ? default
                : new Point(node.X + _graphStyle.Node.Width / 2, node.Y + _graphStyle.Node.Height / 2);
        }
    }

    public Point StartPoint => GetBorderPoint(RawStartPoint, RawEndPoint);
    public Point EndPoint => GetBorderPoint(RawEndPoint, RawStartPoint);
    public Point MidPoint => new((StartPoint.X + EndPoint.X) / 2, (StartPoint.Y + EndPoint.Y) / 2);

    public Edge GetEdge() => _edge;

    private Point GetBorderPoint(Point center, Point otherCenter)
    {
        var dx = otherCenter.X - center.X;
        var dy = otherCenter.Y - center.Y;

        if (Math.Abs(dx) < 0.001 && Math.Abs(dy) < 0.001)
            return center;

        var rx = _graphStyle.Node.Width / 2.0;
        var ry = _graphStyle.Node.Height / 2.0;

        var scale = 1.0 / Math.Sqrt((dx * dx) / (rx * rx) + (dy * dy) / (ry * ry));

        return new Point(
            center.X + dx * scale,
            center.Y + dy * scale);
    }

    private void EdgeOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(e.PropertyName);

        if (e.PropertyName is nameof(Edge.SourceId) or nameof(Edge.TargetId))
            NotifyGeometryChanged();
    }

    private void NodesOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
            foreach (Node node in e.OldItems)
                node.PropertyChanged -= NodeOnPropertyChanged;

        if (e.NewItems is not null)
            foreach (Node node in e.NewItems)
                node.PropertyChanged += NodeOnPropertyChanged;

        NotifyGeometryChanged();
    }

    private void SubscribeToNodes(ObservableCollection<Node> nodes)
    {
        foreach (var node in nodes)
            node.PropertyChanged += NodeOnPropertyChanged;
    }

    private void NodeOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(Node.X) or nameof(Node.Y))
            NotifyGeometryChanged();
    }

    private void GraphNodeStyleOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(NodeStyle.Width) or nameof(NodeStyle.Height))
            NotifyGeometryChanged();
    }

    private void NotifyGeometryChanged()
    {
        OnPropertyChanged(nameof(RawStartPoint));
        OnPropertyChanged(nameof(RawEndPoint));
        OnPropertyChanged(nameof(StartPoint));
        OnPropertyChanged(nameof(EndPoint));
        OnPropertyChanged(nameof(MidPoint));
    }
}