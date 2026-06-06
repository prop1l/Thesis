using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Thesis.Helpers;
using Thesis.Models;
using Thesis.ViewModels;

namespace Thesis.Views;

public partial class GraphEditorWindow : Window
{
    #region Properties

    private GraphEditorViewModel? Vm => DataContext as GraphEditorViewModel;

    #endregion Properties

    #region Fields

    private readonly Dictionary<Node, NodeVisual> _nodes = new();
    private readonly Dictionary<string, EdgeVisual> _edges = new();

    private Node? _dragNode;
    private Point _dragStart;
    private Point _nodeStart;

    private Node? _edgeStartNode;
    private Line? _tempEdgeLine;

    #endregion Fields

    #region Constructor

    public GraphEditorWindow()
    {
        InitializeComponent();

        Opened += OnOpened;
        Closed += OnClosed;

        GraphCanvas.PointerPressed += OnPointerPressed;
        GraphCanvas.PointerMoved += OnPointerMoved;
        GraphCanvas.PointerReleased += OnPointerReleased;
    }

    #endregion Constructor

    #region Lifecycle

    private void OnOpened(object? sender, EventArgs e)
    {
        if (Vm is null)
            return;

        Vm.Nodes.CollectionChanged += OnNodesChanged;
        Vm.Edges.CollectionChanged += OnEdgesChanged;
        Vm.PropertyChanged += OnViewModelPropertyChanged;

        SubscribeToNodes(Vm.Nodes);
        SubscribeToEdges(Vm.Edges);

        RebuildNodes();
        RebuildEdges();

        Dispatcher.UIThread.Post(CenterView, DispatcherPriority.Loaded);
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        if (Vm is null)
            return;

        Vm.Nodes.CollectionChanged -= OnNodesChanged;
        Vm.Edges.CollectionChanged -= OnEdgesChanged;
        Vm.PropertyChanged -= OnViewModelPropertyChanged;

        foreach (var node in Vm.Nodes)
            node.PropertyChanged -= OnNodePropertyChanged;

        foreach (var edge in Vm.Edges)
            edge.PropertyChanged -= OnEdgePropertyChanged;

        CancelEdgeCreation();
        _dragNode = null;
    }

    #endregion Lifecycle

    #region Collection subscriptions

    private void SubscribeToNodes(IEnumerable<Node> nodes)
    {
        foreach (var node in nodes)
            node.PropertyChanged += OnNodePropertyChanged;
    }

    private void SubscribeToEdges(IEnumerable<EdgeViewModel> edges)
    {
        foreach (var edge in edges)
            edge.PropertyChanged += OnEdgePropertyChanged;
    }

    private void OnNodesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (Node node in e.OldItems)
                node.PropertyChanged -= OnNodePropertyChanged;
        }

        if (e.NewItems is not null)
        {
            foreach (Node node in e.NewItems)
                node.PropertyChanged += OnNodePropertyChanged;
        }

        RebuildNodes();
    }

    private void OnEdgesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (EdgeViewModel edge in e.OldItems)
                edge.PropertyChanged -= OnEdgePropertyChanged;
        }

        if (e.NewItems is not null)
        {
            foreach (EdgeViewModel edge in e.NewItems)
                edge.PropertyChanged += OnEdgePropertyChanged;
        }

        RebuildEdges();
    }

    #endregion Collection subscriptions

    #region ViewModel reactions

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (Vm is null)
            return;

        if (e.PropertyName is nameof(GraphEditorViewModel.GraphKind)
            or nameof(GraphEditorViewModel.IsDirectedGraph)
            or nameof(GraphEditorViewModel.IsWeightedGraph)
            or nameof(GraphEditorViewModel.SelectedEdge))
        {
            RefreshAllEdges();
        }
    }

    private void OnNodePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not Node node || !_nodes.TryGetValue(node, out var visual))
            return;

        if (e.PropertyName is nameof(Node.X)
            or nameof(Node.Y)
            or nameof(Node.Label)
            or nameof(Node.IsVisited)
            or nameof(Node.IsHighlighted)
            or nameof(Node.DistanceLabel))
        {
            UpdateNodeVisual(node, visual);
        }

        RepositionConnectedEdges(node);
    }

    private void OnEdgePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not EdgeViewModel edge || !_edges.TryGetValue(edge.Id, out var visual))
            return;

        UpdateEdgeVisual(edge, visual);
    }

    #endregion ViewModel reactions

    #region Rebuild and refresh

    private void RebuildNodes()
    {
        foreach (var item in _nodes.Values)
        {
            GraphCanvas.Children.Remove(item.Ellipse);
            GraphCanvas.Children.Remove(item.Text);
        }

        _nodes.Clear();

        if (Vm is null)
            return;

        foreach (var node in Vm.Nodes)
            AddNodeVisual(node);

        RebuildEdges();
    }

    private void RebuildEdges()
    {
        foreach (var item in _edges.Values)
        {
            GraphCanvas.Children.Remove(item.Line);
            GraphCanvas.Children.Remove(item.ArrowLeft);
            GraphCanvas.Children.Remove(item.ArrowRight);
            GraphCanvas.Children.Remove(item.WeightBorder);
        }

        _edges.Clear();

        if (Vm is null)
            return;

        foreach (var edge in Vm.Edges)
            AddEdgeVisual(edge);

        AddTempEdgeLineIfNeeded();
    }

    private void RefreshAllEdges()
    {
        if (Vm is null)
            return;

        foreach (var edge in Vm.Edges)
        {
            if (_edges.TryGetValue(edge.Id, out var visual))
                UpdateEdgeVisual(edge, visual);
        }
    }

    private void RepositionConnectedEdges(Node node)
    {
        if (Vm is null)
            return;

        foreach (var edge in Vm.Edges.Where(e => e.SourceId == node.Id || e.TargetId == node.Id))
        {
            if (_edges.TryGetValue(edge.Id, out var visual))
                UpdateEdgeVisual(edge, visual);
        }

        if (_edgeStartNode?.Id == node.Id && _tempEdgeLine is not null)
            _tempEdgeLine.StartPoint = GetNodeCenter(node);
    }

    #endregion Rebuild and refresh

    #region Visual creation

    private void AddNodeVisual(Node node)
    {
        var ellipse = new Ellipse();
        var text = new TextBlock
        {
            FontWeight = FontWeight.SemiBold,
            TextAlignment = TextAlignment.Center,
            IsHitTestVisible = false
        };

        var visual = new NodeVisual(ellipse, text);

        UpdateNodeVisual(node, visual);

        GraphCanvas.Children.Add(ellipse);
        GraphCanvas.Children.Add(text);

        _nodes[node] = visual;
    }

    private void AddEdgeVisual(EdgeViewModel edge)
    {
        var line = new Line();
        var arrowLeft = new Line();
        var arrowRight = new Line();

        var weightText = new TextBlock
        {
            FontWeight = FontWeight.SemiBold,
            TextAlignment = TextAlignment.Center,
            IsHitTestVisible = false
        };

        var weightBorder = new Border
        {
            Padding = new Thickness(6, 2),
            CornerRadius = new CornerRadius(8),
            IsHitTestVisible = false,
            Child = weightText
        };

        var visual = new EdgeVisual(line, arrowLeft, arrowRight, weightBorder, weightText);

        UpdateEdgeVisual(edge, visual);

        GraphCanvas.Children.Insert(0, line);
        GraphCanvas.Children.Add(arrowLeft);
        GraphCanvas.Children.Add(arrowRight);
        GraphCanvas.Children.Add(weightBorder);

        _edges[edge.Id] = visual;
    }

    #endregion Visual creation

    #region Visual updates

    private void UpdateNodeVisual(Node node, NodeVisual visual)
    {
        if (Vm is null)
            return;

        var style = Vm.GraphStyle.Node;
        var isSelected = Vm.SelectedNode?.Id == node.Id;
        var isPathNode = Vm.CurrentDijkstraStep?.FinalPathNodeIds.Contains(node.Id) == true;
        var isCurrentNode = Vm.CurrentDijkstraStep?.CurrentNodeId == node.Id;

        IBrush fill;
        IBrush stroke;
        IBrush foreground;
        double strokeThickness = 1.5;

        if (isSelected)
        {
            fill = new SolidColorBrush(Color.Parse("#F1F5F9"));
            stroke = new SolidColorBrush(Color.Parse("#CBD5E1"));
            foreground = new SolidColorBrush(Color.Parse("#334155"));
            strokeThickness = 1.5;
        }
        else if (isCurrentNode)
        {
            fill = new SolidColorBrush(Color.Parse("#2563EB"));
            stroke = new SolidColorBrush(Color.Parse("#1D4ED8"));
            foreground = Brushes.White;
            strokeThickness = 3;
        }
        else if (isPathNode)
        {
            fill = new SolidColorBrush(Color.Parse("#DCFCE7"));
            stroke = new SolidColorBrush(Color.Parse("#22C55E"));
            foreground = new SolidColorBrush(Color.Parse("#14532D"));
            strokeThickness = 2.5;
        }
        else if (node.IsVisited)
        {
            fill = new SolidColorBrush(Color.Parse("#DBEAFE"));
            stroke = new SolidColorBrush(Color.Parse("#60A5FA"));
            foreground = new SolidColorBrush(Color.Parse("#1E3A8A"));
            strokeThickness = 2;
        }
        else
        {
            fill = new SolidColorBrush(Color.Parse("#F1F5F9"));
            stroke = new SolidColorBrush(Color.Parse("#CBD5E1"));
            foreground = new SolidColorBrush(Color.Parse("#334155"));
            strokeThickness = 1.5;
        }

        visual.Ellipse.Width = style.Width;
        visual.Ellipse.Height = style.Height;
        visual.Ellipse.Fill = fill;
        visual.Ellipse.Stroke = stroke;
        visual.Ellipse.StrokeThickness = strokeThickness;

        visual.Text.Width = style.Width;
        visual.Text.Height = style.Height;
        visual.Text.Foreground = foreground;
        visual.Text.Text = string.IsNullOrWhiteSpace(node.DistanceLabel)
            ? node.Label
            : $"{node.Label}\n{node.DistanceLabel}";
        visual.Text.FontSize = string.IsNullOrWhiteSpace(node.DistanceLabel)
            ? style.FontSize
            : Math.Max(11, style.FontSize - 1);
        visual.Text.LineHeight = style.Height / 2;

        SetNodePosition(node, visual.Ellipse, visual.Text);
    }

    private void UpdateEdgeVisual(EdgeViewModel edge, EdgeVisual visual)
    {
        if (Vm is null) return;

        var style = Vm.GraphStyle.Edge;
        var isSelected = Vm.SelectedEdge?.Id == edge.Id;

        IBrush stroke = edge.IsPathEdge
            ? Brushes.SeaGreen
            : edge.IsExamined
                ? Brushes.Orange
                : isSelected
                    ? Brushes.DodgerBlue
                    : BrushHelper.CreateBrush(style.Stroke);

        IBrush foreground = Brushes.Black;
        IBrush background = Brushes.White;

        var thickness = edge.IsPathEdge
            ? style.StrokeThickness + 3
            : edge.IsExamined
                ? style.StrokeThickness + 2
                : isSelected
                    ? style.StrokeThickness + 1
                    : style.StrokeThickness;

        visual.Line.StartPoint = edge.StartPoint;
        visual.Line.EndPoint = edge.EndPoint;
        visual.Line.Stroke = stroke;
        visual.Line.StrokeThickness = thickness;
        visual.Line.StrokeDashArray = style.IsDashed
            ? new Avalonia.Collections.AvaloniaList<double> { 4, 4 }
            : null;

        GraphGeometryHelper.UpdateArrow(
            visual.Line,
            visual.ArrowLeft,
            visual.ArrowRight,
            Vm.IsDirectedGraph,
            stroke,
            thickness);

        visual.WeightBorder.IsVisible = Vm.IsWeightedGraph;
        visual.WeightText.Text = edge.Weight?.ToString() ?? "0";
        visual.WeightText.Foreground = foreground;
        visual.WeightText.FontSize = style.FontSize;
        visual.WeightBorder.Background = background;

        SetEdgeWeightPosition(edge, visual.WeightBorder);
    }

    #endregion Visual updates

    #region Pointer handling

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (Vm is null)
            return;

        var point = e.GetCurrentPoint(GraphCanvas);
        var position = point.Position;

        var node = HitTestNode(position);
        var edge = node is null ? HitTestEdge(position) : null;

        if (point.Properties.IsRightButtonPressed)
        {
            HandleRightClick(node);
            e.Handled = true;
            return;
        }

        if (point.Properties.IsLeftButtonPressed)
        {
            HandleLeftClick(node, edge, position);
            e.Handled = true;
        }
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        var position = e.GetPosition(GraphCanvas);

        if (_edgeStartNode is not null && _tempEdgeLine is not null)
        {
            _tempEdgeLine.StartPoint = GetNodeCenter(_edgeStartNode);
            _tempEdgeLine.EndPoint = position;
            return;
        }

        if (_dragNode is null)
            return;

        var dx = position.X - _dragStart.X;
        var dy = position.Y - _dragStart.Y;

        _dragNode.X = Math.Clamp(_nodeStart.X + dx, 0, 1960);
        _dragNode.Y = Math.Clamp(_nodeStart.Y + dy, 0, 1960);
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (Vm is not null && _dragNode is not null)
            Vm.SaveGraph();

        _dragNode = null;
    }

    private void HandleLeftClick(Node? node, EdgeViewModel? edge, Point position)
    {
        if (Vm is null)
            return;

        if (node is not null)
        {
            Vm.SelectedNode = node;
            Vm.SelectedEdge = null;

            _dragNode = node;
            _dragStart = position;
            _nodeStart = new Point(node.X, node.Y);
            return;
        }

        _dragNode = null;

        if (edge is not null)
        {
            Vm.SelectedEdge = edge;
            Vm.SelectedNode = null;
            return;
        }

        Vm.SelectedNode = null;
        Vm.SelectedEdge = null;
    }

    private void HandleRightClick(Node? node)
    {
        if (Vm is null)
            return;

        if (node is null)
        {
            CancelEdgeCreation();
            return;
        }

        Vm.SelectedNode = node;
        Vm.SelectedEdge = null;

        if (_edgeStartNode is null)
        {
            _edgeStartNode = node;

            _tempEdgeLine = new Line
            {
                StartPoint = GetNodeCenter(node),
                EndPoint = GetNodeCenter(node),
                Stroke = Brushes.DarkOrange,
                StrokeThickness = 2,
                StrokeDashArray = new AvaloniaList<double> { 4, 4 }
            };

            AddTempEdgeLineIfNeeded();
            return;
        }

        if (_edgeStartNode.Id != node.Id)
            Vm.AddEdge(_edgeStartNode, node);

        CancelEdgeCreation();
    }

    #endregion Pointer handling

    #region Hit testing

    private Node? HitTestNode(Point p)
    {
        if (Vm is null)
            return null;

        return GraphHitTestHelper.HitTestNode(
            Vm.Nodes,
            p,
            Vm.GraphStyle.Node.Width,
            Vm.GraphStyle.Node.Height);
    }

    private EdgeViewModel? HitTestEdge(Point p)
    {
        if (Vm is null)
            return null;

        return GraphHitTestHelper.HitTestEdge(Vm.Edges, p);
    }

    #endregion Hit testing

    #region Layout helpers

    private void CenterView()
    {
        if (Vm is null || Vm.Nodes.Count == 0)
            return;

        var minX = Vm.Nodes.Min(x => x.X);
        var minY = Vm.Nodes.Min(x => x.Y);
        var maxX = Vm.Nodes.Max(x => x.X);
        var maxY = Vm.Nodes.Max(x => x.Y);

        var cx = (minX + maxX) / 2;
        var cy = (minY + maxY) / 2;

        GraphScrollViewer.Offset = new Vector(
            Math.Max(0, cx - GraphScrollViewer.Bounds.Width / 2),
            Math.Max(0, cy - GraphScrollViewer.Bounds.Height / 2));
    }

    private Point GetNodeCenter(Node node)
    {
        if (Vm is null)
            return new Point(node.X + 20, node.Y + 20);

        return GraphGeometryHelper.GetNodeCenter(
            node.X,
            node.Y,
            Vm.GraphStyle.Node.Width,
            Vm.GraphStyle.Node.Height);
    }

    private static void SetNodePosition(Node node, Control ellipse, Control text)
    {
        Canvas.SetLeft(ellipse, node.X);
        Canvas.SetTop(ellipse, node.Y);
        Canvas.SetLeft(text, node.X);
        Canvas.SetTop(text, node.Y);
    }

    private static void SetEdgeWeightPosition(EdgeViewModel edge, Control label)
    {
        Canvas.SetLeft(label, edge.MidPoint.X - 18);
        Canvas.SetTop(label, edge.MidPoint.Y - 12);
    }

    #endregion Layout helpers

    #region Temporary edge state

    private void AddTempEdgeLineIfNeeded()
    {
        if (_tempEdgeLine is not null && !GraphCanvas.Children.Contains(_tempEdgeLine))
            GraphCanvas.Children.Insert(0, _tempEdgeLine);
    }

    private void CancelEdgeCreation()
    {
        if (_tempEdgeLine is not null)
        {
            GraphCanvas.Children.Remove(_tempEdgeLine);
            _tempEdgeLine = null;
        }

        _edgeStartNode = null;
    }

    #endregion Temporary edge state
}