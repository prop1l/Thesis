using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Thesis.Algorithms;
using Thesis.Models.Graph;

namespace Thesis.ViewModels.Editor;

/// <summary>
/// Вкладка «Анализ»: характеристики графа, матрица смежности, сравнение алгоритмов поиска пути.
/// </summary>
public partial class GraphEditorViewModel
{
    public const int AnalysisTabIndex = 2;

    [ObservableProperty] private int selectedSideTabIndex;

    // ---------- граф в цифрах ----------

    [ObservableProperty] private string densityText = "—";
    [ObservableProperty] private string averageDegreeText = "—";

    // ---------- матрица смежности ----------

    [ObservableProperty] private IReadOnlyList<string> matrixHeaders = [];
    [ObservableProperty] private IReadOnlyList<MatrixRow> matrixRows = [];

    public string MatrixCaption =>
        (IsWeightedGraph ? "Число в ячейке — вес ребра из строки в столбец" : "1 — есть ребро из строки в столбец") +
        (IsDirectedGraph ? "." : ". Граф неориентированный, поэтому матрица симметрична.");

    // ---------- сравнение алгоритмов ----------

    [ObservableProperty] private IReadOnlyList<AlgorithmComparisonRow> comparisonRows = [];
    [ObservableProperty] private IReadOnlyList<AlgorithmComparisonRow> stepsRanking = [];
    [ObservableProperty] private IReadOnlyList<AlgorithmComparisonRow> visitedRanking = [];
    [ObservableProperty] private IReadOnlyList<string> comparisonInsights = [];
    [ObservableProperty] private string comparisonTitle = string.Empty;

    public bool HasComparison => ComparisonRows.Count > 0;

    /// <summary>Сводка по всем алгоритмам: сложность и свойства.</summary>
    public IReadOnlyList<AlgorithmInfoRow> AlgorithmInfos =>
        Algorithms.Select(a => new AlgorithmInfoRow(a.Name, a.Complexity, a.UsesWeights, a.GuaranteesShortestPath)).ToList();

    [RelayCommand]
    private void BuildPerformanceChart()
    {
        var report = _complexityAnalyzer.BuildComplexityReport();
        ChartData = report.Points;
        PerformanceSummary = report.Summary;
    }

    /// <summary>
    /// Запускает все алгоритмы между выбранными вершинами и сводит результаты:
    /// вес найденного пути, число шагов и обработанных вершин, выводы.
    /// </summary>
    [RelayCommand]
    private void CompareAlgorithms()
    {
        ClearComparison();
        if (SearchStartNode is null || SearchEndNode is null || SearchStartNode == SearchEndNode)
            return;

        var vertices = GetVertices();
        var arcs = GetArcs();
        var results = Algorithms
            .Select(a => (Algorithm: a, Steps: a.BuildSteps(vertices, arcs, IsDirectedGraph, SearchStartNode.Id, SearchEndNode.Id)))
            .ToList();

        // Лучший — наименьший суммарный вес среди алгоритмов, нашедших путь
        var best = results.Select(r => r.Steps[^1].PathCost).Where(c => c is not null).Min();
        var maxSteps = Math.Max(1, results.Max(r => r.Steps.Count));
        var maxVisited = Math.Max(1, results.Max(r => r.Steps[^1].VisitedNodeIds.Count));

        var rows = results.Select(r =>
        {
            var last = r.Steps[^1];
            return new AlgorithmComparisonRow(
                r.Algorithm.Name,
                last.PathCost,
                last.FinalPathEdgeIds.Count,
                last.VisitedNodeIds.Count,
                r.Steps.Count,
                last.PathCost is not null && last.PathCost == best)
            {
                StepsFraction = (double)r.Steps.Count / maxSteps,
                VisitedFraction = (double)last.VisitedNodeIds.Count / maxVisited
            };
        }).ToList();

        ComparisonRows = rows;
        StepsRanking = rows.OrderBy(r => r.StepCount).ToList();
        VisitedRanking = rows.OrderBy(r => r.VisitedCount).ToList();
        ComparisonTitle = $"Путь {SearchStartNode.Label} → {SearchEndNode.Label}";
        ComparisonInsights = BuildInsights(rows, best);
        OnPropertyChanged(nameof(HasComparison));
    }

    /// <summary>«Сравнить все алгоритмы» на вкладке поиска: считает и открывает вкладку «Анализ».</summary>
    [RelayCommand]
    private void ShowComparison()
    {
        CompareAlgorithms();
        SelectedSideTabIndex = AnalysisTabIndex;
    }

    private static IReadOnlyList<string> BuildInsights(List<AlgorithmComparisonRow> rows, decimal? best)
    {
        if (best is null)
            return ["Ни один алгоритм не нашёл путь: конечная вершина недостижима из начальной."];

        var optimal = rows.Where(r => r.IsBest).ToList();
        var fastest = optimal.MinBy(r => r.StepCount)!;
        var slowest = optimal.MaxBy(r => r.StepCount)!;
        var worse = rows.Where(r => r.Found && !r.IsBest).OrderBy(r => r.PathCost).ToList();

        var insights = new List<string>
        {
            $"Минимальный вес пути — {SearchRun.Format(best.Value)}: его нашли {optimal.Count} из {rows.Count} алгоритмов.",
            optimal.Count > 1
                ? $"Среди них меньше всего шагов у «{fastest.Name}» ({fastest.StepCount}), больше всего — у «{slowest.Name}» ({slowest.StepCount})."
                : $"Его нашёл только «{fastest.Name}» за {fastest.StepCount} шагов."
        };

        // Главное преимущество A*: тот же оптимальный ответ за меньшее число шагов
        var dijkstra = rows.FirstOrDefault(r => r.Name == new DijkstraSolver().Name);
        var astar = rows.FirstOrDefault(r => r.Name == new AStarSearch().Name);
        if (dijkstra is { IsBest: true } && astar is { IsBest: true } && astar.StepCount < dijkstra.StepCount)
            insights.Add($"A* потребовалось {astar.StepCount} шагов против {dijkstra.StepCount} у Дейкстры — " +
                         $"на {1 - (double)astar.StepCount / dijkstra.StepCount:P0} меньше благодаря оценке расстояния до цели.");

        if (worse.Count > 0)
            insights.Add("Путь тяжелее минимального: " +
                         string.Join(", ", worse.Select(r => $"{r.Name} — {r.CostText}")) + ".");

        var fewestVisited = rows.Where(r => r.Found).MinBy(r => r.VisitedCount)!;
        insights.Add($"Меньше всего вершин обработал «{fewestVisited.Name}» — {fewestVisited.VisitedCount}.");

        return insights;
    }

    private void ClearComparison()
    {
        ComparisonRows = [];
        StepsRanking = [];
        VisitedRanking = [];
        ComparisonInsights = [];
        OnPropertyChanged(nameof(HasComparison));
    }

    // ---------- матрица и статистика ----------

    public string GetAdjacencyMatrixText() =>
        AdjacencyMatrix.BuildText(GetVertices(), GetArcs(), IsDirectedGraph, IsWeightedGraph);

    /// <summary>Пересчитывает матрицу смежности и числовые характеристики графа.</summary>
    private void UpdateAdjacencyMatrix()
    {
        var vertices = GetVertices();
        var arcs = GetArcs();
        var matrix = AdjacencyMatrix.Build(vertices, arcs, IsDirectedGraph, IsWeightedGraph);

        MatrixHeaders = vertices.Select(v => v.Label).ToList();
        MatrixRows = vertices.Select((v, i) => new MatrixRow(v.Label,
            vertices.Select((_, j) => i == j && matrix[i, j] == 0
                ? new MatrixCell("—", false, true)
                : new MatrixCell(matrix[i, j].ToString("0.##", CultureInfo.CurrentCulture), matrix[i, j] != 0, false)).ToList())).ToList();

        var v = vertices.Count;
        var e = arcs.Count;
        var maxEdges = v * (v - 1) / (IsDirectedGraph ? 1.0 : 2.0);
        DensityText = maxEdges > 0 ? $"{e / maxEdges:P0}" : "—";
        AverageDegreeText = v > 0 ? (IsDirectedGraph ? (double)e / v : 2.0 * e / v).ToString("0.#", CultureInfo.CurrentCulture) : "—";
        OnPropertyChanged(nameof(MatrixCaption));
    }
}

public sealed record MatrixCell(string Text, bool HasEdge, bool IsDiagonal);

public sealed record MatrixRow(string Header, IReadOnlyList<MatrixCell> Cells);

/// <summary>Строка сравнения алгоритмов на текущем графе.</summary>
public record AlgorithmComparisonRow(
    string Name, decimal? PathCost, int EdgeCount, int VisitedCount, int StepCount, bool IsBest)
{
    public bool Found => PathCost is not null;
    public string CostText => PathCost is { } c ? SearchRun.Format(c) : "—";
    public string EdgesText => Found ? EdgeCount.ToString() : "—";

    /// <summary>Доля от максимума — длина полоски на диаграмме (0…1).</summary>
    public double StepsFraction { get; init; }
    public double VisitedFraction { get; init; }
}

public sealed record AlgorithmInfoRow(string Name, string Complexity, bool UsesWeights, bool GuaranteesShortestPath)
{
    public string WeightsText => UsesWeights ? "по весам" : "по рёбрам";
    public string GuaranteeText => GuaranteesShortestPath ? "кратчайший" : "любой путь";
}
