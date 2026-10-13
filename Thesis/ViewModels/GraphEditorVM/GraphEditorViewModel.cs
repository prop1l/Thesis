using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Thesis.Algorithms;
using Thesis.Models;
using Thesis.Models.Algorithms;
using Thesis.Models.Graph;
using Thesis.Services;

namespace Thesis.ViewModels;

public partial class GraphEditorViewModel : ObservableObject
{
    private readonly IGraphRepository _repository;
    private readonly IDijkstraSolver _dijkstraSolver;
    private readonly IComplexityAnalyzer _complexityAnalyzer;
    private string _currentGraphName = string.Empty;
    private readonly Dictionary<string, decimal> _logicalEdgeWeights = new();
    private double _zoomLevel = 1.0;
    private bool _isGraphLoaded;

    [ObservableProperty] private string graphName = string.Empty;
    [ObservableProperty] private ObservableCollection<Node> nodes = new();
    [ObservableProperty] private ObservableCollection<EdgeViewModel> edges = new();
    [ObservableProperty] private Node? selectedNode;
    [ObservableProperty] private EdgeViewModel? selectedEdge;
    [ObservableProperty] private GraphStyle graphStyle = new();
    [ObservableProperty] private GraphKind graphKind = GraphKind.UndirectedUnweighted;
    [ObservableProperty] private GraphKindOption? selectedGraphKindOption;

    [ObservableProperty] private Node? dijkstraStartNode;
    [ObservableProperty] private Node? dijkstraEndNode;
    [ObservableProperty] private ObservableCollection<DijkstraStep> dijkstraSteps = new();
    [ObservableProperty] private int currentDijkstraStepIndex = -1;
    [ObservableProperty] private bool isDijkstraMode;
    [ObservableProperty] private string dijkstraStatus = "Алгоритм не запущен";

    [ObservableProperty] private string performanceSummary = string.Empty;

    [ObservableProperty] private string adjacencyMatrixText = string.Empty;

    private List<PerformancePoint> _chartData = new();
    public List<PerformancePoint> ChartData
    {
        get => _chartData;
        set => SetProperty(ref _chartData, value);
    }

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
        Edges.CollectionChanged += (_, _) => NotifyState();

        SubscribeToGraphStyle(GraphStyle);
        SelectedGraphKindOption = GraphKindOptions.FirstOrDefault(x => x.Value == GraphKind);
    }

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
        var key = GetUndirectedPairKey(sourceId, targetId);
        _logicalEdgeWeights[key] = weight;
    }

    private void SyncLinkedEdgeWeights(string sourceId, string targetId, decimal weight)
    {
        var linkedEdges = Edges.Where(e =>
            (e.SourceId == sourceId && e.TargetId == targetId) ||
            (e.SourceId == targetId && e.TargetId == sourceId));

        foreach (var edge in linkedEdges)
            edge.Weight = weight;
    }

    [RelayCommand]
    private void BuildPerformanceChart()
    {
        var report = _complexityAnalyzer.BuildComplexityReport();
        ChartData = report.Points.ToList();
        PerformanceSummary = report.Summary;
    }

    [RelayCommand]
    private void BuildAdjacencyMatrix()
    {
        AdjacencyMatrixText = GetAdjacencyMatrixText();
    }

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
        {
            node.IsHighlighted = false;
            node.IsVisited = false;
            node.DistanceLabel = string.Empty;
        }

        foreach (var edge in Edges)
        {
            edge.IsHighlighted = false;
            edge.IsExamined = false;
            edge.IsPathEdge = false;
        }

        OnPropertyChanged(nameof(CurrentDijkstraStep));
        OnPropertyChanged(nameof(CanGoToNextStep));
        OnPropertyChanged(nameof(CanGoToPreviousStep));
    }

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

    public void LoadGraph(string graphName)
    {
        _isGraphLoaded = false;

        _currentGraphName = graphName;
        GraphName = graphName;

        var data = _repository.LoadGraph(graphName);

        Nodes.Clear();
        Edges.Clear();
        ClearDijkstra();

        if (data is null)
        {
            GraphStyle = new GraphStyle();
            GraphKind = GraphKind.UndirectedUnweighted;
            SelectedGraphKindOption = GraphKindOptions.First(x => x.Value == GraphKind);
            SubscribeToGraphStyle(GraphStyle);
            _isGraphLoaded = true;
            return;
        }

        GraphStyle = data.Style ?? new GraphStyle();
        GraphKind = data.Kind;
        SelectedGraphKindOption = GraphKindOptions.FirstOrDefault(x => x.Value == GraphKind);

        SubscribeToGraphStyle(GraphStyle);

        foreach (var node in data.Nodes)
            Nodes.Add(node);

        foreach (var edge in data.Edges)
            Edges.Add(new EdgeViewModel(edge, Nodes, GraphStyle));

        RebuildLogicalWeights();

        NotifyState();
        OnPropertyChanged(nameof(GraphStyle));
        OnPropertyChanged(nameof(GraphKind));
        OnPropertyChanged(nameof(IsDirectedGraph));
        OnPropertyChanged(nameof(IsWeightedGraph));

        _isGraphLoaded = true;
        AdjacencyMatrixText = GetAdjacencyMatrixText();
    }

    [RelayCommand]
    private void AddNode()
    {
        AddNodeAt(100 + Nodes.Count * 30, 100 + Nodes.Count * 30);
    }

    public void AddNodeAt(double x, double y)
    {
        Nodes.Add(new Node
        {
            Label = $"Вершина {Nodes.Count + 1}",
            X = Math.Clamp(x, 0, 1960),
            Y = Math.Clamp(y, 0, 1960)
        });

        SaveGraph();
        AdjacencyMatrixText = GetAdjacencyMatrixText();
    }

    public void AddEdge(Node source, Node target)
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

        decimal? weight = null;

        if (IsWeightedGraph)
        {
            var logicalWeight = GetLogicalWeight(source.Id, target.Id);
            weight = logicalWeight;
        }

        var edge = new Edge
        {
            SourceId = source.Id,
            TargetId = target.Id,
            Label = $"Ребро {Edges.Count + 1}",
            Weight = weight
        };

        var vm = new EdgeViewModel(edge, Nodes, GraphStyle);
        Edges.Add(vm);

        if (IsWeightedGraph && weight.HasValue)
            SyncLinkedEdgeWeights(source.Id, target.Id, weight.Value);

        SelectedEdge = vm;
        SelectedNode = null;

        SaveGraph();
        AdjacencyMatrixText = GetAdjacencyMatrixText();
    }

    [RelayCommand]
    private void DeleteAll()
    {
        Nodes.Clear();
        Edges.Clear();
        SelectedNode = null;
        SelectedEdge = null;
        ClearDijkstra();
        SaveGraph();
        AdjacencyMatrixText = GetAdjacencyMatrixText();
    }

    [RelayCommand]
    private void DeleteSelected()
    {
        if (SelectedNode is not null)
        {
            var node = SelectedNode;
            var related = Edges
                .Where(e => e.SourceId == node.Id || e.TargetId == node.Id)
                .ToList();

            foreach (var edge in related)
                Edges.Remove(edge);

            Nodes.Remove(node);
            SelectedNode = null;
        }
        else if (SelectedEdge is not null)
        {
            Edges.Remove(SelectedEdge);
            SelectedEdge = null;
        }

        ClearDijkstra();
        SaveGraph();
        AdjacencyMatrixText = GetAdjacencyMatrixText();
    }

    [RelayCommand]
    private void Save()
    {
        SaveGraph();
    }

    public void UpdateSelectedEdgeWeight(decimal? newWeight)
    {
        if (SelectedEdge is null || !IsWeightedGraph || newWeight is null)
            return;

        SetLogicalWeight(SelectedEdge.SourceId, SelectedEdge.TargetId, newWeight.Value);
        SyncLinkedEdgeWeights(SelectedEdge.SourceId, SelectedEdge.TargetId, newWeight.Value);
        SaveGraph();
        AdjacencyMatrixText = GetAdjacencyMatrixText();
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
                Y = n.Y,
                IsVisited = n.IsVisited,
                IsHighlighted = n.IsHighlighted,
                DistanceLabel = n.DistanceLabel
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

    partial void OnSelectedNodeChanged(Node? value)
    {
        if (value is not null && SelectedEdge is not null)
            SelectedEdge = null;

        NotifySelection();
    }

    partial void OnSelectedEdgeChanged(EdgeViewModel? value)
    {
        if (value is not null && SelectedNode is not null)
            SelectedNode = null;

        NotifySelection();
    }

    partial void OnGraphStyleChanged(GraphStyle value)
    {
        SubscribeToGraphStyle(value);
        OnPropertyChanged(nameof(GraphStyle));
        SaveGraph();
    }

    partial void OnGraphKindChanged(GraphKind value)
    {
        SelectedGraphKindOption = GraphKindOptions.FirstOrDefault(x => x.Value == value);

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

    partial void OnDijkstraStartNodeChanged(Node? value)
    {
        OnPropertyChanged(nameof(CanRunDijkstra));
    }

    partial void OnDijkstraEndNodeChanged(Node? value)
    {
        OnPropertyChanged(nameof(CanRunDijkstra));
    }

    partial void OnCurrentDijkstraStepIndexChanged(int value)
    {
        OnPropertyChanged(nameof(CurrentDijkstraStep));
        OnPropertyChanged(nameof(CanGoToNextStep));
        OnPropertyChanged(nameof(CanGoToPreviousStep));
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
        OnPropertyChanged(nameof(GraphStyle));
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

    private void RebuildLogicalWeights()
    {
        _logicalEdgeWeights.Clear();

        foreach (var edge in Edges.Where(e => e.Weight.HasValue))
        {
            var key = GetUndirectedPairKey(edge.SourceId, edge.TargetId);

            if (!_logicalEdgeWeights.ContainsKey(key))
                _logicalEdgeWeights[key] = edge.Weight!.Value;
        }
    }

    public string GetAdjacencyMatrixText() =>
        AdjacencyMatrix.BuildText(GetVertices(), GetArcs(), IsDirectedGraph, IsWeightedGraph);

    private List<GraphVertex> GetVertices() =>
        Nodes.Select(n => new GraphVertex(n.Id, n.Label)).ToList();

    private List<GraphArc> GetArcs() =>
        Edges.Select(e => new GraphArc(e.Id, e.SourceId, e.TargetId, e.Label, e.Weight)).ToList();

    private void ApplyCurrentDijkstraStep()
    {
        var step = CurrentDijkstraStep;
        if (step is null)
            return;

        DijkstraStatus = step.Description;

        foreach (var node in Nodes)
        {
            node.IsVisited = step.VisitedNodeIds.Contains(node.Id);
            node.IsHighlighted = node.Id == step.CurrentNodeId || step.FinalPathNodeIds.Contains(node.Id);

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

        OnPropertyChanged(nameof(CurrentDijkstraStep));
        OnPropertyChanged(nameof(CanGoToNextStep));
        OnPropertyChanged(nameof(CanGoToPreviousStep));
    }
}

public class GraphKindOption
{
    public GraphKind Value { get; set; }
    public string Title { get; set; } = string.Empty;
}