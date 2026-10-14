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
using Thesis.Models.Graph;
using Thesis.Services;

namespace Thesis.ViewModels.Editor;

public partial class GraphEditorViewModel : ObservableObject
{
    private readonly IGraphRepository _repository;
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

    [ObservableProperty] private NodeViewModel? searchStartNode;
    [ObservableProperty] private NodeViewModel? searchEndNode;
    [ObservableProperty] private int currentStepIndex = -1;
    [ObservableProperty] private bool isSearchMode;
    [ObservableProperty] private string searchStatus = "Алгоритм не запущен";
    [ObservableProperty] private IPathFindingAlgorithm selectedAlgorithm;

    [ObservableProperty] private string performanceSummary = string.Empty;
    [ObservableProperty] private IReadOnlyList<PerformancePoint> chartData = [];


    public ObservableCollection<NodeViewModel> Nodes { get; } = new();
    public ObservableCollection<EdgeViewModel> Edges { get; } = new();
    public ObservableCollection<SearchStep> SearchSteps { get; } = new();

    /// <summary>Алгоритмы поиска пути, доступные в редакторе.</summary>
    public IReadOnlyList<IPathFindingAlgorithm> Algorithms { get; }


    public string AlgorithmPropertiesText => SelectedAlgorithm is null ? string.Empty :
        $"{(SelectedAlgorithm.UsesWeights ? "Учитывает веса рёбер" : "Веса не учитывает: каждое ребро = 1")} · " +
        $"{(SelectedAlgorithm.GuaranteesShortestPath ? "гарантирует кратчайший путь" : "путь может быть не кратчайшим")}";

    public ObservableCollection<GraphKindOption> GraphKindOptions { get; } = new(
        Enum.GetValues<GraphKind>().Select(kind => new GraphKindOption { Value = kind, Title = kind.ToTitle() }));

    public string GraphKindTitle => GraphKind.ToTitle();

    public bool IsDirectedGraph =>
        GraphKind is GraphKind.DirectedUnweighted or GraphKind.DirectedWeighted;

    public bool IsWeightedGraph =>
        GraphKind is GraphKind.DirectedWeighted or GraphKind.UndirectedWeighted;

    /// <summary>
    /// Область, занятая вершинами (в координатах графа, могут быть отрицательными).
    /// По ней View подбирает размер холста; у пустого графа — null.
    /// </summary>
    public Rect? ContentBounds
    {
        get
        {
            if (Nodes.Count == 0)
                return null;

            var left = Nodes.Min(n => n.X);
            var top = Nodes.Min(n => n.Y);
            return new Rect(left, top, Nodes.Max(n => n.X + n.Width) - left, Nodes.Max(n => n.Y + n.Height) - top);
        }
    }

    public int NodesCount => Nodes.Count;
    public bool IsGraphEmpty => Nodes.Count == 0;
    public int EdgesCount => Edges.Count;
    public bool IsNodeSelected => SelectedNode is not null;
    public bool IsEdgeSelected => SelectedEdge is not null;
    public bool IsNothingSelected => SelectedNode is null && SelectedEdge is null;

    public bool CanGoToPreviousStep => CurrentStepIndex > 0;
    public bool CanGoToNextStep => CurrentStepIndex >= 0 && CurrentStepIndex < SearchSteps.Count - 1;
    public bool CanRunSearch => SearchStartNode is not null && SearchEndNode is not null && SearchStartNode != SearchEndNode;

    public double ZoomLevel
    {
        get => _zoomLevel;
        set
        {
            value = Math.Clamp(value, MinZoom, MaxZoom);

            // Без округления на каждом шаге (оно копится: 12 щелчков туда-обратно давали 100,5 %),
            // но после возврата к исходному масштабу ровно 100 % вместо 1.0000000000000002
            if (Math.Abs(value - 1.0) < 1e-6)
                value = 1.0;

            SetProperty(ref _zoomLevel, value);
        }
    }

    public const double MinZoom = 0.25;
    public const double MaxZoom = 2.0;

    /// <summary>«Шаг 3 из 17» — чтобы было понятно, где мы в ходе алгоритма.</summary>
    public string StepCounterText => CurrentStep is null
        ? string.Empty
        : $"Шаг {CurrentStepIndex + 1} из {SearchSteps.Count}";

    public double StepProgress => SearchSteps.Count == 0
        ? 0
        : (CurrentStepIndex + 1) * 100.0 / SearchSteps.Count;

    public SearchStep? CurrentStep =>
        CurrentStepIndex >= 0 && CurrentStepIndex < SearchSteps.Count
            ? SearchSteps[CurrentStepIndex]
            : null;

    public GraphEditorViewModel(
        IGraphRepository repository,
        IReadOnlyList<IPathFindingAlgorithm> algorithms,
        IComplexityAnalyzer complexityAnalyzer)
    {
        _repository = repository;
        Algorithms = algorithms;
        selectedAlgorithm = algorithms[0];
        _complexityAnalyzer = complexityAnalyzer;

        Nodes.CollectionChanged += OnNodesCollectionChanged;
        Edges.CollectionChanged += OnEdgesCollectionChanged;

        SubscribeToGraphStyle(GraphStyle);
        SelectedGraphKindOption = GraphKindOptions.FirstOrDefault(x => x.Value == GraphKind);

        // График теоретической сложности не зависит от графа — строим сразу, чтобы вкладка не была пустой
        BuildPerformanceChart();
    }

    #region Loading and saving

    public void LoadGraph(string graphName)
    {
        _isGraphLoaded = false;

        _currentGraphName = graphName;
        GraphName = graphName;

        var data = _repository.LoadGraph(graphName);

        ClearSearch();
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
        ResetHistory();
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

    /// <summary>Двойной клик по холсту: вершина появляется центром в точке клика.</summary>
    public void AddNodeAtCenter(Point center)
    {
        AddNodeAt(center.X - GraphStyle.Node.Width / 2, center.Y - GraphStyle.Node.Height / 2);
    }

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
            X = x,
            Y = y
        };

        Nodes.Add(new NodeViewModel(node, GraphStyle.Node));

        SaveGraph();
        UpdateAdjacencyMatrix();
        RecordChange();
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
        RecordChange();
    }

    [RelayCommand]
    private void DeleteAll()
    {
        SelectedNode = null;
        SelectedEdge = null;
        ClearSearch();
        ClearGraph();
        SaveGraph();
        UpdateAdjacencyMatrix();
        RecordChange();
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

        ClearSearch();
        SaveGraph();
        UpdateAdjacencyMatrix();
        RecordChange();
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
        {
            node.PropertyChanged -= OnNodePropertyChanged;
            node.Detach();
        }

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

    private void OnNodesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (NodeViewModel node in e.OldItems)
                node.PropertyChanged -= OnNodePropertyChanged;
        }

        if (e.NewItems is not null)
        {
            foreach (NodeViewModel node in e.NewItems)
                node.PropertyChanged += OnNodePropertyChanged;
        }

        NotifyState();
        OnPropertyChanged(nameof(ContentBounds));
    }

    private void OnNodePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not NodeViewModel node)
            return;

        switch (e.PropertyName)
        {
            case nameof(NodeViewModel.X):
            case nameof(NodeViewModel.Y):
                OnPropertyChanged(nameof(ContentBounds));
                RecordChange($"move:{node.Id}");
                break;
            case nameof(NodeViewModel.Width):
            case nameof(NodeViewModel.Height):
                OnPropertyChanged(nameof(ContentBounds));
                break;
            case nameof(NodeViewModel.Label):
                UpdateAdjacencyMatrix();
                RecordChange($"label:{node.Id}");
                break;
        }
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
        if (e.PropertyName == nameof(EdgeViewModel.Label) && sender is EdgeViewModel labeledEdge)
        {
            RecordChange($"edge-label:{labeledEdge.Id}");
            return;
        }

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
        RecordChange($"weight:{edge.Id}");
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

        // Холст бесконечный, поэтому координаты ничем не ограничены (в том числе могут уйти в минус)
        _dragNode.X = _nodeStart.X + position.X - _dragStart.X;
        _dragNode.Y = _nodeStart.Y + position.Y - _dragStart.Y;
    }

    public void OnCanvasPointerReleased()
    {
        if (_dragNode is not null)
            SaveGraph();

        _dragNode = null;
        EndChangeGroup();
    }

    public void CancelEdgeCreation()
    {
        _edgeStartNode = null;
        IsCreatingEdge = false;
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

    /// <summary>Ctrl + колёсико: каждый «щелчок» меняет масштаб в 1.1 раза.</summary>
    public void ZoomByWheel(double wheelDelta)
    {
        ZoomLevel *= Math.Pow(1.1, wheelDelta);
    }

    #endregion

    #region Path search

    [RelayCommand]
    private void RunSearch()
    {
        if (SearchStartNode is null || SearchEndNode is null)
            return;

        var steps = SelectedAlgorithm.BuildSteps(
            GetVertices(), GetArcs(), IsDirectedGraph, SearchStartNode.Id, SearchEndNode.Id);

        SearchSteps.Clear();
        foreach (var step in steps)
            SearchSteps.Add(step);

        CurrentStepIndex = SearchSteps.Count > 0 ? 0 : -1;
        IsSearchMode = SearchSteps.Count > 0;
        SearchStatus = CurrentStep?.Description ?? "Алгоритм не запущен";

        ApplyCurrentStep();
    }

    [RelayCommand]
    private void NextStep()
    {
        if (CurrentStepIndex >= SearchSteps.Count - 1)
            return;

        CurrentStepIndex++;
        ApplyCurrentStep();
    }

    [RelayCommand]
    private void PreviousStep()
    {
        if (CurrentStepIndex <= 0)
            return;

        CurrentStepIndex--;
        ApplyCurrentStep();
    }

    [RelayCommand]
    private void ClearSearch()
    {
        SearchSteps.Clear();
        ClearComparison();
        CurrentStepIndex = -1;
        IsSearchMode = false;
        SearchStatus = "Алгоритм не запущен";

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

    private void ApplyCurrentStep()
    {
        var step = CurrentStep;
        if (step is null)
            return;

        SearchStatus = step.Description;

        foreach (var node in Nodes)
        {
            node.IsVisited = step.VisitedNodeIds.Contains(node.Id);
            node.IsCurrent = node.Id == step.CurrentNodeId;
            node.IsOnPath = step.FinalPathNodeIds.Contains(node.Id);
            node.IsInFrontier = step.FrontierNodeIds.Contains(node.Id);

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

    #region Graph snapshot

    private List<GraphVertex> GetVertices() =>
        Nodes.Select(n => new GraphVertex(n.Id, n.Label, n.Center.X, n.Center.Y)).ToList();

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

        ClearSearch();

        OnPropertyChanged(nameof(IsDirectedGraph));
        OnPropertyChanged(nameof(IsWeightedGraph));
        OnPropertyChanged(nameof(GraphKindTitle));
        UpdateAdjacencyMatrix();
        SaveGraph();
        RecordChange();
    }

    partial void OnSelectedGraphKindOptionChanged(GraphKindOption? value)
    {
        if (value is not null && GraphKind != value.Value)
            GraphKind = value.Value;
    }

    partial void OnSearchStartNodeChanged(NodeViewModel? value)
    {
        OnPropertyChanged(nameof(CanRunSearch));
    }

    partial void OnSelectedAlgorithmChanged(IPathFindingAlgorithm value)
    {
        // Шаги прошлого алгоритма к новому не относятся
        ClearSearch();
        OnPropertyChanged(nameof(AlgorithmPropertiesText));
    }

    partial void OnSearchEndNodeChanged(NodeViewModel? value)
    {
        OnPropertyChanged(nameof(CanRunSearch));
    }

    partial void OnCurrentStepIndexChanged(int value)
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
        OnPropertyChanged(nameof(IsGraphEmpty));
        OnPropertyChanged(nameof(EdgesCount));
        OnPropertyChanged(nameof(CanRunSearch));
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
        OnPropertyChanged(nameof(CurrentStep));
        OnPropertyChanged(nameof(CanGoToNextStep));
        OnPropertyChanged(nameof(CanGoToPreviousStep));
        OnPropertyChanged(nameof(StepCounterText));
        OnPropertyChanged(nameof(StepProgress));
    }

    #endregion
}

public class GraphKindOption
{
    public GraphKind Value { get; set; }
    public string Title { get; set; } = string.Empty;
}
