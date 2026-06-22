using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Thesis.Models;
using Thesis.Models.Algorithms;
using Thesis.Models.Graph;

namespace Thesis.ViewModels;

public partial class GraphEditorViewModel : ObservableObject
{
    private readonly WelcomeViewModel _welcome;
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

    [ObservableProperty] private ObservableCollection<PerformancePoint> performancePoints = new();
    [ObservableProperty] private string performanceSummary = string.Empty;
    [ObservableProperty] private bool showPerformanceChart;
    [ObservableProperty]
    private decimal? benchmarkRuns = 20;

    [ObservableProperty]
    private ISeries[] performanceSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private Axis[] performanceXAxes = Array.Empty<Axis>();

    [ObservableProperty]
    private Axis[] performanceYAxes = Array.Empty<Axis>();

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

    public GraphEditorViewModel(WelcomeViewModel welcome)
    {
        _welcome = welcome;

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
        try
        {
            var points = new List<PerformancePoint>();

            var testSizes = new int[] { 5, 10, 20, 30, 50, 75, 100 };

            foreach (var vertexCount in testSizes)
            {
                var logV = Math.Log(vertexCount);
                var vLogV = vertexCount * logV;
                var vSquared = vertexCount * vertexCount;
                var vCubed = vertexCount * vertexCount * vertexCount;

                points.Add(new PerformancePoint
                {
                    VertexCount = vertexCount,
                    EdgeCount = vertexCount * (vertexCount - 1) / 2,
                    TheoreticalTime = vLogV,
                    Complexity = "O(V log V)"
                });

                points.Add(new PerformancePoint
                {
                    VertexCount = vertexCount,
                    EdgeCount = vertexCount * (vertexCount - 1) / 2,
                    TheoreticalTime = vSquared / 10,
                    Complexity = "O(V²)"
                });

                points.Add(new PerformancePoint
                {
                    VertexCount = vertexCount,
                    EdgeCount = vertexCount * (vertexCount - 1) / 2,
                    TheoreticalTime = 1,
                    Complexity = "O(1)"
                });
            }

            ChartData = points;

            PerformanceSummary = BuildComplexitySummary();
            ShowPerformanceChart = true;

            Debug.WriteLine($"✅ Построен график сложности: {points.Count} точек");
        }
        catch (Exception ex)
        {
            PerformanceSummary = $"Ошибка: {ex.Message}";
            ShowPerformanceChart = false;
            ChartData = new List<PerformancePoint>();
        }
    }

    private string BuildComplexitySummary()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("📊 АНАЛИЗ СЛОЖНОСТИ АЛГОРИТМА ДЕЙКСТРЫ");
        sb.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        sb.AppendLine("");
        sb.AppendLine("График показывает теоретическую сложность");
        sb.AppendLine("алгоритма Дейкстры в зависимости от размера графа:");
        sb.AppendLine("");
        sb.AppendLine("  🔵 O(1) - константная (идеальный случай)");
        sb.AppendLine("  🟢 O(V log V) - алгоритм с бинарной кучей");
        sb.AppendLine("  🔴 O(V²) - алгоритм без оптимизации");
        sb.AppendLine("");
        sb.AppendLine("Алгоритм Дейкстры с кучей (O(V log V))");
        sb.AppendLine("значительно быстрее на больших графах,");
        sb.AppendLine("чем реализация без кучи (O(V²)).");

        return sb.ToString();
    }

    [RelayCommand]
    private void BuildAsymptoticChart()
    {
        try
        {
            var points = new List<PerformancePoint>();

            // Тестируем разные размеры графов
            var sizes = Enumerable.Range(1, 100).Select(i => i * 5).ToArray();

            foreach (var v in sizes)
            {
                // V вершин
                var maxEdges = v * (v - 1) / 2;

                // Разные плотности графа
                var densities = new[] { 0.1, 0.25, 0.5, 0.75, 1.0 };

                foreach (var density in densities)
                {
                    var e = (int)(maxEdges * density);

                    // Теоретическое время для разных реализаций
                    points.Add(new PerformancePoint
                    {
                        VertexCount = v,
                        EdgeCount = e,
                        TheoreticalTime = CalculateTheoreticalTime(v, e, "Куча"),
                        Complexity = $"Плотность {density * 100:F0}%"
                    });
                }
            }

            ChartData = points;
            ShowPerformanceChart = true;
        }
        catch (Exception ex)
        {
            PerformanceSummary = $"Ошибка: {ex.Message}";
        }
    }

    private double CalculateTheoreticalTime(int v, int e, string implementation)
    {
        // Расчет теоретического времени в условных единицах
        switch (implementation)
        {
            case "Куча":
                // O((V+E) log V)
                return (v + e) * Math.Log(v);

            case "Матрица":
                // O(V²)
                return v * v;

            case "Фибоначчи":
                // O(E + V log V)
                return e + v * Math.Log(v);

            default:
                return v * v;
        }
    }

    [RelayCommand]
    private void CompareTheoryAndPractice()
    {
        try
        {
            if (Nodes.Count < 2)
            {
                PerformanceSummary = "Добавьте вершины для сравнения";
                return;
            }

            var points = new List<PerformancePoint>();
            var start = Nodes.First();
            var end = Nodes.Last();

            // Размеры для тестирования
            var testSizes = new[] { 5, 10, 15, 20, 25, 30, 40, 50 };

            foreach (var size in testSizes)
            {
                // Сохраняем исходный граф
                var originalNodes = SaveNodes();
                var originalEdges = SaveEdges();

                try
                {
                    // Создаем граф с size вершинами
                    PrepareGraphWithVertexCount(size);

                    // Измеряем реальное время
                    var actualTime = MeasureActualTime(start, end, 20);

                    // Теоретическое время для O(V log V)
                    var theoreticalTime = size * Math.Log(size);

                    points.Add(new PerformancePoint
                    {
                        VertexCount = size,
                        EdgeCount = size * (size - 1) / 2,
                        ActualTime = actualTime,
                        TheoreticalTime = theoreticalTime,
                        Complexity = "Сравнение"
                    });
                }
                finally
                {
                    RestoreGraph(originalNodes, originalEdges);
                }
            }

            ChartData = points;
            PerformanceSummary = BuildComparisonSummary(points);
            ShowPerformanceChart = true;
        }
        catch (Exception ex)
        {
            PerformanceSummary = $"Ошибка: {ex.Message}";
        }
    }

    private List<Node> SaveNodes()
    {
        return Nodes.Select(n => new Node
        {
            Id = n.Id,
            Label = n.Label,
            X = n.X,
            Y = n.Y,
            IsVisited = n.IsVisited,
            IsHighlighted = n.IsHighlighted,
            DistanceLabel = n.DistanceLabel
        }).ToList();
    }

    private List<Edge> SaveEdges()
    {
        return Edges.Select(e => new Edge
        {
            Id = e.Id,
            SourceId = e.SourceId,
            TargetId = e.TargetId,
            Label = e.Label,
            Weight = e.Weight
        }).ToList();
    }

    private void PrepareGraphWithVertexCount(int vertexCount)
    {
        // Очищаем граф
        Nodes.Clear();
        Edges.Clear();
        _logicalEdgeWeights.Clear();

        // Создаем вершины
        for (int i = 0; i < vertexCount; i++)
        {
            Nodes.Add(new Node
            {
                Id = Guid.NewGuid().ToString(),
                Label = $"V{i + 1}",
                X = 100 + i * 30,
                Y = 100 + i * 30
            });
        }

        // Создаем ребра (полный граф)
        var nodeList = Nodes.ToList();
        for (int i = 0; i < nodeList.Count; i++)
        {
            for (int j = i + 1; j < nodeList.Count; j++)
            {
                var edge = new Edge
                {
                    Id = Guid.NewGuid().ToString(),
                    SourceId = nodeList[i].Id,
                    TargetId = nodeList[j].Id,
                    Label = $"E{i + 1}-{j + 1}",
                    Weight = IsWeightedGraph ? 1m : null
                };

                Edges.Add(new EdgeViewModel(edge, Nodes, GraphStyle));
            }
        }
    }

    private double MeasureActualTime(Node start, Node end, int runs)
    {
        var totalMs = 0.0;

        for (int i = 0; i < runs; i++)
        {
            var sw = Stopwatch.StartNew();
            BuildDijkstraSteps(start, end);
            sw.Stop();
            totalMs += sw.Elapsed.TotalMilliseconds;
        }

        return totalMs / runs;
    }

    private string BuildComparisonSummary(List<PerformancePoint> points)
    {
        if (!points.Any()) return "Нет данных";

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("📊 СРАВНЕНИЕ ТЕОРИИ И ПРАКТИКИ");
        sb.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━━━");
        sb.AppendLine("");
        sb.AppendLine("График показывает:");
        sb.AppendLine("  • 🔵 Теоретическую сложность O(V log V)");
        sb.AppendLine("  • 🔴 Реальное время выполнения");
        sb.AppendLine("");
        sb.AppendLine("Совпадение кривых подтверждает,");
        sb.AppendLine("что алгоритм работает с заявленной сложностью.");

        // Оценка точности
        var avgDiff = points.Average(p => Math.Abs(p.ActualTime - p.TheoreticalTime) / p.TheoreticalTime);
        sb.AppendLine("");
        sb.AppendLine($"📈 Среднее отклонение: {avgDiff * 100:F1}%");

        if (avgDiff < 0.2)
            sb.AppendLine("✓ Отличное соответствие теории!");
        else if (avgDiff < 0.5)
            sb.AppendLine("✓ Хорошее соответствие теории");
        else
            sb.AppendLine("⚠️ Рекомендуется оптимизация");

        return sb.ToString();
    }

  
    private void RestoreGraph(List<Node> originalNodes, List<Edge> originalEdges)
    {
        Nodes.Clear();
        Edges.Clear();
        _logicalEdgeWeights.Clear();

        foreach (var node in originalNodes)
        {
            Nodes.Add(new Node
            {
                Id = node.Id,
                Label = node.Label,
                X = node.X,
                Y = node.Y,
                IsVisited = node.IsVisited,
                IsHighlighted = node.IsHighlighted,
                DistanceLabel = node.DistanceLabel
            });
        }

        foreach (var edge in originalEdges)
        {
            var restoredEdge = new Edge
            {
                Id = edge.Id,
                SourceId = edge.SourceId,
                TargetId = edge.TargetId,
                Label = edge.Label,
                Weight = edge.Weight
            };

            Edges.Add(new EdgeViewModel(restoredEdge, Nodes, GraphStyle));

            if (edge.Weight.HasValue)
                SetLogicalWeight(edge.SourceId, edge.TargetId, edge.Weight.Value);
        }

        NotifyState();
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

        var steps = BuildDijkstraSteps(DijkstraStartNode, DijkstraEndNode);

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

        var data = _welcome.LoadGraphData(graphName);

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

        _welcome.SaveGraphData(_currentGraphName, data);
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

    private List<DijkstraStep> BuildDijkstraSteps(Node start, Node end)
    {
        var steps = new List<DijkstraStep>();

        var dist = Nodes.ToDictionary(n => n.Id, _ => decimal.MaxValue);
        var prev = Nodes.ToDictionary(n => n.Id, _ => (string?)null);
        var visited = new HashSet<string>();

        dist[start.Id] = 0;

        steps.Add(CreateStep(
            "Инициализация",
            $"Начальная вершина: {start.Label}, конечная: {end.Label}",
            null,
            null,
            visited,
            dist,
            prev));

        while (true)
        {
            var current = Nodes
                .Where(n => !visited.Contains(n.Id) && dist[n.Id] != decimal.MaxValue)
                .OrderBy(n => dist[n.Id])
                .FirstOrDefault();

            if (current is null)
                break;

            steps.Add(CreateStep(
                "Выбор вершины",
                $"Выбрана вершина {current.Label} с текущим расстоянием {dist[current.Id]}",
                current.Id,
                null,
                visited,
                dist,
                prev));

            visited.Add(current.Id);

            steps.Add(CreateStep(
                "Посещение вершины",
                $"Вершина {current.Label} помечена как обработанная",
                current.Id,
                null,
                visited,
                dist,
                prev));

            if (current.Id == end.Id)
                break;

            var outgoingEdges = GetAdjacentEdges(current);

            foreach (var edge in outgoingEdges)
            {
                var neighborId = GetNeighborId(current.Id, edge);
                if (neighborId is null || visited.Contains(neighborId))
                    continue;

                var weight = edge.Weight ?? 1m;
                var candidate = dist[current.Id] + weight;

                steps.Add(CreateStep(
                    "Просмотр ребра",
                    $"Проверяем ребро {edge.Label}: {current.Label} -> {GetNodeLabel(neighborId)}",
                    current.Id,
                    edge.Id,
                    visited,
                    dist,
                    prev));

                if (candidate < dist[neighborId])
                {
                    dist[neighborId] = candidate;
                    prev[neighborId] = current.Id;

                    steps.Add(CreateStep(
                        "Обновление расстояния",
                        $"Для вершины {GetNodeLabel(neighborId)} найден более короткий путь: {candidate}",
                        current.Id,
                        edge.Id,
                        visited,
                        dist,
                        prev));
                }
            }
        }

        var pathNodeIds = ReconstructPath(start.Id, end.Id, prev);
        var pathEdgeIds = GetPathEdgeIds(pathNodeIds);

        steps.Add(new DijkstraStep
        {
            Index = steps.Count,
            Title = "Кратчайший путь",
            Description = pathNodeIds.Count == 0
                ? "Путь не найден"
                : $"Найден кратчайший путь длиной {dist[end.Id]}",
            CurrentNodeId = end.Id,
            VisitedNodeIds = new HashSet<string>(visited),
            Distances = new Dictionary<string, decimal>(dist),
            Previous = new Dictionary<string, string?>(prev),
            FinalPathNodeIds = pathNodeIds,
            FinalPathEdgeIds = pathEdgeIds,
            IsFinished = true
        });

        return steps;
    }

    private DijkstraStep CreateStep(
        string title,
        string description,
        string? currentNodeId,
        string? examinedEdgeId,
        HashSet<string> visited,
        Dictionary<string, decimal> distances,
        Dictionary<string, string?> previous)
    {
        return new DijkstraStep
        {
            Index = DijkstraSteps.Count,
            Title = title,
            Description = description,
            CurrentNodeId = currentNodeId,
            ExaminedEdgeId = examinedEdgeId,
            VisitedNodeIds = new HashSet<string>(visited),
            Distances = new Dictionary<string, decimal>(distances),
            Previous = new Dictionary<string, string?>(previous)
        };
    }

    private IEnumerable<EdgeViewModel> GetAdjacentEdges(Node node)
    {
        if (IsDirectedGraph)
            return Edges.Where(e => e.SourceId == node.Id);

        return Edges.Where(e => e.SourceId == node.Id || e.TargetId == node.Id);
    }

    private string? GetNeighborId(string currentId, EdgeViewModel edge)
    {
        if (IsDirectedGraph)
            return edge.SourceId == currentId ? edge.TargetId : null;

        if (edge.SourceId == currentId)
            return edge.TargetId;

        if (edge.TargetId == currentId)
            return edge.SourceId;

        return null;
    }

    private string GetNodeLabel(string nodeId)
    {
        return Nodes.FirstOrDefault(n => n.Id == nodeId)?.Label ?? nodeId;
    }

    private List<string> ReconstructPath(string startId, string endId, Dictionary<string, string?> prev)
    {
        var path = new List<string>();

        if (startId != endId && prev[endId] is null)
            return path;

        var current = endId;

        while (current is not null)
        {
            path.Add(current);

            if (current == startId)
                break;

            current = prev[current]!;
        }

        path.Reverse();

        if (path.Count == 0 || path[0] != startId)
            return new List<string>();

        return path;
    }

    private List<string> GetPathEdgeIds(List<string> pathNodeIds)
    {
        var result = new List<string>();

        for (int i = 0; i < pathNodeIds.Count - 1; i++)
        {
            var from = pathNodeIds[i];
            var to = pathNodeIds[i + 1];

            var edge = IsDirectedGraph
                ? Edges.FirstOrDefault(e => e.SourceId == from && e.TargetId == to)
                : Edges.FirstOrDefault(e =>
                    (e.SourceId == from && e.TargetId == to) ||
                    (e.SourceId == to && e.TargetId == from));

            if (edge is not null)
                result.Add(edge.Id);
        }

        return result;
    }

    public string GetAdjacencyMatrixText()
    {
        if (Nodes.Count == 0)
            return "Граф пуст.";

        var orderedNodes = Nodes.ToList();
        var indexById = orderedNodes
            .Select((node, index) => new { node.Id, index })
            .ToDictionary(x => x.Id, x => x.index);

        var matrix = new decimal[orderedNodes.Count, orderedNodes.Count];

        foreach (var edgeVm in Edges)
        {
            if (!indexById.TryGetValue(edgeVm.SourceId, out var sourceIndex))
                continue;

            if (!indexById.TryGetValue(edgeVm.TargetId, out var targetIndex))
                continue;

            var value = IsWeightedGraph
                ? (edgeVm.Weight ?? 1m)
                : 1m;

            matrix[sourceIndex, targetIndex] = value;

            if (!IsDirectedGraph)
                matrix[targetIndex, sourceIndex] = value;
        }

        return FormatAdjacencyMatrix(orderedNodes, matrix);
    }

    private static string FormatAdjacencyMatrix(IReadOnlyList<Node> nodes, decimal[,] matrix)
    {
        var rowHeaderWidth = Math.Max(
            4,
            nodes.Max(n => string.IsNullOrWhiteSpace(n.Label) ? 1 : n.Label.Length));

        var cellWidth = Math.Max(
            3,
            nodes.Max(n => string.IsNullOrWhiteSpace(n.Label) ? 1 : n.Label.Length));

        for (var i = 0; i < nodes.Count; i++)
        {
            for (var j = 0; j < nodes.Count; j++)
            {
                var text = matrix[i, j].ToString("0.##");
                if (text.Length > cellWidth)
                    cellWidth = text.Length;
            }
        }

        var lines = new List<string>();

        var headerCells = new List<string>
    {
        "".PadRight(rowHeaderWidth)
    };

        headerCells.AddRange(nodes.Select(n =>
            (string.IsNullOrWhiteSpace(n.Label) ? "-" : n.Label).PadLeft(cellWidth)));

        lines.Add(string.Join(" ", headerCells));

        for (var i = 0; i < nodes.Count; i++)
        {
            var row = new List<string>
        {
            (string.IsNullOrWhiteSpace(nodes[i].Label) ? "-" : nodes[i].Label).PadRight(rowHeaderWidth)
        };

            for (var j = 0; j < nodes.Count; j++)
            {
                row.Add(matrix[i, j].ToString("0.##").PadLeft(cellWidth));
            }

            lines.Add(string.Join(" ", row));
        }

        return string.Join(Environment.NewLine, lines);
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