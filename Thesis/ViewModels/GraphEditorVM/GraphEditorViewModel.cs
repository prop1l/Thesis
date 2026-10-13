using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Thesis.Algorithms;
using Thesis.Helpers;
using Thesis.Models;
using Thesis.Models.Algorithms;
using Thesis.Models.Graph;
using Thesis.Services;

namespace Thesis.ViewModels;

public partial class GraphEditorViewModel : ObservableObject
{
    private const double CanvasMaxCoordinate = 1960;

    private readonly IGraphRepository _repository;
    private readonly IDijkstraSolver _dijkstraSolver;
    private readonly IComplexityAnalyzer _complexityAnalyzer;
    private readonly Dictionary<string, decimal> _logicalEdgeWeights = new();
    private string _currentGraphName = string.Empty;
    private double _zoomLevel = 1.0;
    private bool _isGraphLoaded;

    // Пока true, изменения веса рёбер не синхронизируются и не сохраняются
    // (массовая смена весов при смене типа графа или синхронизации парных рёбер)
    private bool _suppressWeightSync;

    [ObservableProperty] private string graphName = string.Empty;
    [ObservableProperty] private NodeViewModel? selectedNode;
    [ObservableProperty] private EdgeViewModel? selectedEdge;
    [ObservableProperty] private GraphStyle graphStyle = new();
    [ObservableProperty] private GraphKind graphKind = GraphKind.UndirectedUnweighted;
    [ObservableProperty] private GraphKindOption? selectedGraphKindOption;

    [ObservableProperty] private NodeViewModel? dijkstraStartNode;
    [ObservableProperty] private NodeViewModel? dijkstraEndNode;
    [ObservableProperty] private int currentDijkstraStepIndex = -1;
    [ObservableProperty] private bool isDijkstraMode;
    [ObservableProperty] private string dijkstraStatus = "Алгоритм не запущен";

    [ObservableProperty] private string performanceSummary = string.Empty;
    [ObservableProperty] private IReadOnlyList<PerformancePoint> chartData = [];

    [ObservableProperty] private string adjacencyMatrixText = string.Empty;

    public ObservableCollection<NodeViewModel> Nodes { get; } = new();
    public ObservableCollection<EdgeViewModel> Edges { get; } = new();
    public ObservableCollection<DijkstraStep> DijkstraSteps { get; } = new();

    public ObservableCollection<GraphKindOption> GraphKindOptions { get; } = new()
    {
        new GraphKindOption { Value = GraphKind.UndirectedUnweighted, Title = "Неориентированный невзвешенный" },
        new GraphKindOption { Value = GraphKind.UndirectedWeighted, Title = "Неориентированный взвешенный" },
        new GraphKindOption { Value = GraphKind.DirectedUnweighted, Title = "Ориентированный невзвешенный" },
        new GraphKindOption { Value = GraphKind.DirectedWeighted, Title = "Ориентированный взвешенный" }
    };

    public bool IsDirectedGraph =>
        GraphKind is GraphKind.DirectedUnweighted or GraphKind.DirectedWeighted;

    public bool IsWeightedGraph =>
        GraphKind is GraphKind.DirectedWeighted or GraphKind.UndirectedWeighted;

    public int NodesCount => Nodes.Count;
    public int EdgesCount => Edges.Count;
    public bool IsNodeSelected => SelectedNode is not null;
    public bool IsEdgeSelected => SelectedEdge is not null;
    public bool IsNothingSelected => SelectedNode is null && SelectedEdge is null;

    public bool CanGoToPreviousStep => CurrentDijkstraStepIndex > 0;
    public bool CanGoToNextStep => CurrentDijkstraStepIndex >= 0 && CurrentDijkstraStepIndex < DijkstraSteps.Count - 1;
    public bool CanRunDijkstra => DijkstraStartNode is not null && DijkstraEndNode is not null && DijkstraStartNode != DijkstraEndNode;

    public double ZoomLevel
    {
        get => _zoomLevel;
        set => SetProperty(ref _zoomLevel, Math.Clamp(value, 0.5, 1.5));
    }

    public DijkstraStep? CurrentDijkstraStep =>
        CurrentDijkstraStepIndex >= 0 && CurrentDijkstraStepIndex < DijkstraSteps.Count
            ? DijkstraSteps[CurrentDijkstraStepIndex]
            : null;

    public GraphEditorViewModel(
        IGraphRepository repository,
        IDijkstraSolver dijkstraSolver,
        IComplexityAnalyzer complexityAnalyzer)
    {
        _repository = repository;
        _dijkstraSolver = dijkstraSolver;
        _complexityAnalyzer = complexityAnalyzer;

        Nodes.CollectionChanged += (_, _) => NotifyState();
        Edges.CollectionChanged += OnEdgesCollectionChanged;

        SubscribeToGraphStyle(GraphStyle);
        SelectedGraphKindOption = GraphKindOptions.FirstOrDefault(x => x.Value == GraphKind);
    }

    #region Loading and saving

    public void LoadGraph(string graphName)
    {
        _isGraphLoaded = false;

        _currentGraphName = graphName;
        GraphName = graphName;

        var data = _repository.LoadGraph(graphName);

        ClearDijkstra();
        ClearGraph();

        GraphStyle = data.Style ?? new GraphStyle();
        GraphKind = data.Kind;
        SelectedGraphKindOption = GraphKindOptions.FirstOrDefault(x => x.Value == GraphKind);

        foreach (var node in data.Nodes)
            Nodes.Add(new NodeViewModel(node, GraphStyle.Node));

        var nodesById = Nodes.ToDictionary(n => n.Id);

        foreach (var edge in data.Edges)
        {
            // Ребро на несуществующую вершину (повреждённый файл) пропускаем
            if (nodesById.TryGetValue(edge.SourceId, out var source) &&
                nodesById.TryGetValue(edge.TargetId, out var target))
            {
                Edges.Add(new EdgeViewModel(edge, source, target, GraphStyle));
            }
        }

        RebuildLogicalWeights();

        NotifyState();
        OnPropertyChanged(nameof(IsDirectedGraph));
        OnPropertyChanged(nameof(IsWeightedGraph));

        _isGraphLoaded = true;
        UpdateAdjacencyMatrix();
    }

    public void SaveGraph()
    {
        if (!_isGraphLoaded || string.IsNullOrWhiteSpace(_currentGraphName))
            return;

        var data = new GraphData
        {
            Name = GraphName,
            Nodes = new ObservableCollection<Node>(Nodes.Select(n => new Node
            {
                Id = n.Id,
                Label = n.Label,
                X = n.X,
                Y = n.Y
            })),
            Edges = new ObservableCollection<Edge>(Edges.Select(e => new Edge
            {
                Id = e.Id,
                SourceId = e.SourceId,
                TargetId = e.TargetId,
                Label = e.Label,
                Weight = e.Weight
            })),
            Style = GraphStyle,
            Kind = GraphKind,
            LastModified = DateTime.Now
        };

        _repository.SaveGraph(_currentGraphName, data);
    }

    [RelayCommand]
    private void Save()
    {
        SaveGraph();
    }

    #endregion

    #region Editing

    [RelayCommand]
    private void AddNode()
    {
        AddNodeAt(100 + Nodes.Count * 30, 100 + Nodes.Count * 30);
    }

    public void AddNodeAt(double x, double y)
    {
        var node = new Node
        {
            Label = $"Вершина {Nodes.Count + 1}",
            X = Math.Clamp(x, 0, CanvasMaxCoordinate),
            Y = Math.Clamp(y, 0, CanvasMaxCoordinate)
        };

        Nodes.Add(new NodeViewModel(node, GraphStyle.Node));

        SaveGraph();
        UpdateAdjacencyMatrix();
    }

    public void AddEdge(NodeViewModel source, NodeViewModel target)
    {
        if (source.Id == target.Id)
            return;

        var exists = IsDirectedGraph
            ? Edges.Any(e => e.SourceId == source.Id && e.TargetId == target.Id)
            : Edges.Any(e =>
                (e.SourceId == source.Id && e.TargetId == target.Id) ||
                (e.SourceId == target.Id && e.TargetId == source.Id));

        if (exists)
            return;

        // Ребро между той же парой вершин (например, обратное в орграфе) получает тот же вес
        decimal? weight = IsWeightedGraph ? GetLogicalWeight(source.Id, target.Id) : null;

        var edge = new Edge
        {
            SourceId = source.Id,
            TargetId = target.Id,
            Label = $"Ребро {Edges.Count + 1}",
            Weight = weight
        };

        var vm = new EdgeViewModel(edge, source, target, GraphStyle);
        Edges.Add(vm);

        if (weight.HasValue)
            SyncLinkedEdgeWeights(source.Id, target.Id, weight.Value);

        SelectedEdge = vm;

        SaveGraph();
        UpdateAdjacencyMatrix();
    }

    [RelayCommand]
    private void DeleteAll()
    {
        SelectedNode = null;
        SelectedEdge = null;
        ClearDijkstra();
        ClearGraph();
        SaveGraph();
        UpdateAdjacencyMatrix();
    }

    [RelayCommand]
    private void DeleteSelected()
    {
        if (SelectedNode is not null)
        {
            var node = SelectedNode;
            SelectedNode = null;

            foreach (var edge in Edges.Where(e => e.SourceId == node.Id || e.TargetId == node.Id).ToList())
                Edges.Remove(edge);

            Nodes.Remove(node);
            node.Detach();
        }
        else if (SelectedEdge is not null)
        {
            var edge = SelectedEdge;
            SelectedEdge = null;
            Edges.Remove(edge);
        }

        ClearDijkstra();
        SaveGraph();
        UpdateAdjacencyMatrix();
    }

    public void UpdateSelectedEdgeWeight(decimal? newWeight)
    {
        if (SelectedEdge is null || !IsWeightedGraph || newWeight is null)
            return;

        // Остальное (синхронизация парных рёбер и сохранение) делает OnEdgePropertyChanged —
        // тот же путь, что и при правке веса в панели свойств
        SelectedEdge.Weight = newWeight;
    }

    private void ClearGraph()
    {
        // Clear() не передаёт удалённые элементы в CollectionChanged, поэтому отписываемся вручную
        foreach (var edge in Edges)
        {
            edge.PropertyChanged -= OnEdgePropertyChanged;
            edge.Detach();
        }

        foreach (var node in Nodes)
            node.Detach();

        Edges.Clear();
        Nodes.Clear();
        _logicalEdgeWeights.Clear();
    }

    #endregion

    #region Edge weights

    private static string GetUndirectedPairKey(string sourceId, string targetId)
    {
        return string.CompareOrdinal(sourceId, targetId) < 0
            ? $"{sourceId}|{targetId}"
            : $"{targetId}|{sourceId}";
    }

    private decimal GetLogicalWeight(string sourceId, string targetId)
    {
        var key = GetUndirectedPairKey(sourceId, targetId);
        return _logicalEdgeWeights.TryGetValue(key, out var weight) ? weight : 1m;
    }

    private void SetLogicalWeight(string sourceId, string targetId, decimal weight)
    {
        _logicalEdgeWeights[GetUndirectedPairKey(sourceId, targetId)] = weight;
    }

    private void SyncLinkedEdgeWeights(string sourceId, string targetId, decimal weight)
    {
        _suppressWeightSync = true;
        try
        {
            var linkedEdges = Edges.Where(e =>
                (e.SourceId == sourceId && e.TargetId == targetId) ||
                (e.SourceId == targetId && e.TargetId == sourceId));

            foreach (var edge in linkedEdges)
                edge.Weight = weight;
        }
        finally
        {
            _suppressWeightSync = false;
        }
    }

    private void RebuildLogicalWeights()
    {
        _logicalEdgeWeights.Clear();

        foreach (var edge in Edges.Where(e => e.Weight.HasValue))
            _logicalEdgeWeights.TryAdd(GetUndirectedPairKey(edge.SourceId, edge.TargetId), edge.Weight!.Value);
    }

    private void OnEdgesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (EdgeViewModel edge in e.OldItems)
            {
                edge.PropertyChanged -= OnEdgePropertyChanged;
                edge.Detach();
            }
        }

        if (e.NewItems is not null)
        {
            foreach (EdgeViewModel edge in e.NewItems)
                edge.PropertyChanged += OnEdgePropertyChanged;
        }

        NotifyState();
    }

    private void OnEdgePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_suppressWeightSync ||
            e.PropertyName != nameof(EdgeViewModel.Weight) ||
            sender is not EdgeViewModel edge ||
            !IsWeightedGraph ||
            edge.Weight is not { } weight)
            return;

        SetLogicalWeight(edge.SourceId, edge.TargetId, weight);
        SyncLinkedEdgeWeights(edge.SourceId, edge.TargetId, weight);
        SaveGraph();
        UpdateAdjacencyMatrix();
    }

    #endregion

    #region Canvas interaction

    // Методы ниже вызываются из code-behind окна при событиях мыши.
    // Вся логика (выделение, перетаскивание, создание рёбер) живёт здесь и покрыта тестами.

    private NodeViewModel? _dragNode;
    private Point _dragStart;
    private Point _nodeStart;
    private NodeViewModel? _edgeStartNode;

    [ObservableProperty] private bool isCreatingEdge;
    [ObservableProperty] private Point pendingEdgeStart;
    [ObservableProperty] private Point pendingEdgeEnd;

    /// <summary>ЛКМ: выделить вершину и начать её перетаскивание, либо выделить ребро, либо снять выделение.</summary>
    public void OnCanvasLeftPressed(Point position)
    {
        var node = GraphHitTestHelper.HitTestNode(Nodes, position);

        if (node is not null)
        {
            SelectedNode = node;

            _dragNode = node;
            _dragStart = position;
            _nodeStart = new Point(node.X, node.Y);
            return;
        }

        _dragNode = null;

        var edge = GraphHitTestHelper.HitTestEdge(Edges, position);

        if (edge is not null)
        {
            SelectedEdge = edge;
            return;
        }

        SelectedNode = null;
        SelectedEdge = null;
    }

    /// <summary>ПКМ по вершине: первый клик начинает ребро, второй — создаёт его. ПКМ по пустому месту — отмена.</summary>
    public void OnCanvasRightPressed(Point position)
    {
        var node = GraphHitTestHelper.HitTestNode(Nodes, position);

        if (node is null)
        {
            CancelEdgeCreation();
            return;
        }

        SelectedNode = node;

        if (_edgeStartNode is null)
        {
            _edgeStartNode = node;
            PendingEdgeStart = node.Center;
            PendingEdgeEnd = node.Center;
            IsCreatingEdge = true;
            return;
        }

        if (_edgeStartNode.Id != node.Id)
            AddEdge(_edgeStartNode, node);

        CancelEdgeCreation();
    }

    public void OnCanvasPointerMoved(Point position)
    {
        if (_edgeStartNode is not null)
        {
            PendingEdgeStart = _edgeStartNode.Center;
            PendingEdgeEnd = position;
            return;
        }

        if (_dragNode is null)
            return;

        _dragNode.X = Math.Clamp(_nodeStart.X + position.X - _dragStart.X, 0, CanvasMaxCoordinate);
        _dragNode.Y = Math.Clamp(_nodeStart.Y + position.Y - _dragStart.Y, 0, CanvasMaxCoordinate);
    }

    public void OnCanvasPointerReleased()
    {
        if (_dragNode is not null)
            SaveGraph();

        _dragNode = null;
    }

    public void CancelEdgeCreation()
    {
        _edgeStartNode = null;
        IsCreatingEdge = false;
    }

    /// <summary>Центр области, занятой вершинами, — чтобы прокрутить к графу при открытии окна.</summary>
    public Point? GetGraphCenter()
    {
        if (Nodes.Count == 0)
            return null;

        return new Point(
            (Nodes.Min(n => n.X) + Nodes.Max(n => n.X)) / 2,
            (Nodes.Min(n => n.Y) + Nodes.Max(n => n.Y)) / 2);
    }

    #endregion

    #region Zoom

    [RelayCommand]
    private void ZoomIn()
    {
        ZoomLevel += 0.1;
    }

    [RelayCommand]
    private void ZoomOut()
    {
        ZoomLevel -= 0.1;
    }

    [RelayCommand]
    private void ResetZoom()
    {
        ZoomLevel = 1.0;
    }

    #endregion

    #region Dijkstra

    [RelayCommand]
    private void RunDijkstra()
    {
        if (DijkstraStartNode is null || DijkstraEndNode is null)
            return;

        var steps = _dijkstraSolver.BuildSteps(
            GetVertices(), GetArcs(), IsDirectedGraph, DijkstraStartNode.Id, DijkstraEndNode.Id);

        DijkstraSteps.Clear();
        foreach (var step in steps)
            DijkstraSteps.Add(step);

        CurrentDijkstraStepIndex = DijkstraSteps.Count > 0 ? 0 : -1;
        IsDijkstraMode = DijkstraSteps.Count > 0;
        DijkstraStatus = CurrentDijkstraStep?.Description ?? "Алгоритм не запущен";

        ApplyCurrentDijkstraStep();
    }

    [RelayCommand]
    private void NextDijkstraStep()
    {
        if (CurrentDijkstraStepIndex >= DijkstraSteps.Count - 1)
            return;

        CurrentDijkstraStepIndex++;
        ApplyCurrentDijkstraStep();
    }

    [RelayCommand]
    private void PreviousDijkstraStep()
    {
        if (CurrentDijkstraStepIndex <= 0)
            return;

        CurrentDijkstraStepIndex--;
        ApplyCurrentDijkstraStep();
    }

    [RelayCommand]
    private void ClearDijkstra()
    {
        DijkstraSteps.Clear();
        CurrentDijkstraStepIndex = -1;
        IsDijkstraMode = false;
        DijkstraStatus = "Алгоритм не запущен";

        foreach (var node in Nodes)
            node.ResetAlgorithmState();

        foreach (var edge in Edges)
        {
            edge.IsHighlighted = false;
            edge.IsExamined = false;
            edge.IsPathEdge = false;
        }

        NotifyStepState();
    }

    private void ApplyCurrentDijkstraStep()
    {
        var step = CurrentDijkstraStep;
        if (step is null)
            return;

        DijkstraStatus = step.Description;

        foreach (var node in Nodes)
        {
            node.IsVisited = step.VisitedNodeIds.Contains(node.Id);
            node.IsCurrent = node.Id == step.CurrentNodeId;
            node.IsOnPath = step.FinalPathNodeIds.Contains(node.Id);

            node.DistanceLabel = step.Distances.TryGetValue(node.Id, out var distance) && distance != decimal.MaxValue
                ? distance.ToString()
                : "∞";
        }

        foreach (var edge in Edges)
        {
            edge.IsExamined = edge.Id == step.ExaminedEdgeId;
            edge.IsPathEdge = step.FinalPathEdgeIds.Contains(edge.Id);
            edge.IsHighlighted = edge.IsExamined || edge.IsPathEdge;
        }

        NotifyStepState();
    }

    #endregion

    #region Analysis

    [RelayCommand]
    private void BuildPerformanceChart()
    {
        var report = _complexityAnalyzer.BuildComplexityReport();
        ChartData = report.Points;
        PerformanceSummary = report.Summary;
    }

    [RelayCommand]
    private void BuildAdjacencyMatrix()
    {
        UpdateAdjacencyMatrix();
    }

    public string GetAdjacencyMatrixText() =>
        AdjacencyMatrix.BuildText(GetVertices(), GetArcs(), IsDirectedGraph, IsWeightedGraph);

    private void UpdateAdjacencyMatrix()
    {
        AdjacencyMatrixText = GetAdjacencyMatrixText();
    }

    private List<GraphVertex> GetVertices() =>
        Nodes.Select(n => new GraphVertex(n.Id, n.Label)).ToList();

    private List<GraphArc> GetArcs() =>
        Edges.Select(e => new GraphArc(e.Id, e.SourceId, e.TargetId, e.Label, e.Weight)).ToList();

    #endregion

    #region Property change hooks

    partial void OnSelectedNodeChanged(NodeViewModel? oldValue, NodeViewModel? newValue)
    {
        if (oldValue is not null)
            oldValue.IsSelected = false;

        if (newValue is not null)
        {
            newValue.IsSelected = true;
            SelectedEdge = null;
        }

        NotifySelection();
    }

    partial void OnSelectedEdgeChanged(EdgeViewModel? oldValue, EdgeViewModel? newValue)
    {
        if (oldValue is not null)
            oldValue.IsSelected = false;

        if (newValue is not null)
        {
            newValue.IsSelected = true;
            SelectedNode = null;
        }

        NotifySelection();
    }

    partial void OnGraphStyleChanged(GraphStyle value)
    {
        SubscribeToGraphStyle(value);
        SaveGraph();
    }

    partial void OnGraphKindChanged(GraphKind value)
    {
        SelectedGraphKindOption = GraphKindOptions.FirstOrDefault(x => x.Value == value);

        _suppressWeightSync = true;
        try
        {
            if (!IsWeightedGraph)
            {
                foreach (var edge in Edges)
                    edge.Weight = null;
            }
            else
            {
                foreach (var edge in Edges.Where(e => e.Weight is null))
                    edge.Weight = 1;
            }
        }
        finally
        {
            _suppressWeightSync = false;
        }

        ClearDijkstra();

        OnPropertyChanged(nameof(IsDirectedGraph));
        OnPropertyChanged(nameof(IsWeightedGraph));
        SaveGraph();
    }

    partial void OnSelectedGraphKindOptionChanged(GraphKindOption? value)
    {
        if (value is not null && GraphKind != value.Value)
            GraphKind = value.Value;
    }

    partial void OnDijkstraStartNodeChanged(NodeViewModel? value)
    {
        OnPropertyChanged(nameof(CanRunDijkstra));
    }

    partial void OnDijkstraEndNodeChanged(NodeViewModel? value)
    {
        OnPropertyChanged(nameof(CanRunDijkstra));
    }

    partial void OnCurrentDijkstraStepIndexChanged(int value)
    {
        NotifyStepState();
    }

    private void SubscribeToGraphStyle(GraphStyle style)
    {
        style.PropertyChanged -= OnGraphStylePropertyChanged;
        style.Node.PropertyChanged -= OnGraphStylePropertyChanged;
        style.Edge.PropertyChanged -= OnGraphStylePropertyChanged;

        style.PropertyChanged += OnGraphStylePropertyChanged;
        style.Node.PropertyChanged += OnGraphStylePropertyChanged;
        style.Edge.PropertyChanged += OnGraphStylePropertyChanged;
    }

    private void OnGraphStylePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        SaveGraph();
    }

    private void NotifyState()
    {
        OnPropertyChanged(nameof(NodesCount));
        OnPropertyChanged(nameof(EdgesCount));
        OnPropertyChanged(nameof(CanRunDijkstra));
        NotifySelection();
    }

    private void NotifySelection()
    {
        OnPropertyChanged(nameof(IsNodeSelected));
        OnPropertyChanged(nameof(IsEdgeSelected));
        OnPropertyChanged(nameof(IsNothingSelected));
    }

    private void NotifyStepState()
    {
        OnPropertyChanged(nameof(CurrentDijkstraStep));
        OnPropertyChanged(nameof(CanGoToNextStep));
        OnPropertyChanged(nameof(CanGoToPreviousStep));
    }

    #endregion
}

public class GraphKindOption
{
    public GraphKind Value { get; set; }
    public string Title { get; set; } = string.Empty;
}
