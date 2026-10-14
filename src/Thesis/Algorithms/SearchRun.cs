using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Thesis.Algorithms;

/// <summary>
/// Состояние одного запуска поиска: граф в удобном виде, метки вершин, дерево предков
/// и список записанных шагов. Общие операции вынесены сюда, чтобы каждый алгоритм
/// содержал только собственную логику.
/// </summary>
public sealed class SearchRun
{
    private readonly Dictionary<string, List<(GraphArc Arc, string To)>> _out = new();
    private readonly Dictionary<string, List<(GraphArc Arc, string To)>> _in = new();
    private readonly Dictionary<string, string> _labels;

    public SearchRun(IReadOnlyList<GraphVertex> vertices, IReadOnlyList<GraphArc> arcs, bool isDirected,
        string startId, string endId, bool usesWeights)
    {
        Vertices = vertices;
        Arcs = arcs;
        IsDirected = isDirected;
        StartId = startId;
        EndId = endId;
        UsesWeights = usesWeights;
        _labels = vertices.ToDictionary(v => v.Id, v => v.Label);

        foreach (var v in vertices)
        {
            _out[v.Id] = new();
            _in[v.Id] = new();
            Distances[v.Id] = decimal.MaxValue;
            Previous[v.Id] = null;
            PreviousArc[v.Id] = null;
        }

        foreach (var arc in arcs)
        {
            if (!_out.ContainsKey(arc.SourceId) || !_out.ContainsKey(arc.TargetId))
                continue;

            _out[arc.SourceId].Add((arc, arc.TargetId));
            _in[arc.TargetId].Add((arc, arc.SourceId));

            // Неориентированное ребро проходимо в обе стороны
            if (!isDirected)
            {
                _out[arc.TargetId].Add((arc, arc.SourceId));
                _in[arc.SourceId].Add((arc, arc.TargetId));
            }
        }
    }

    public IReadOnlyList<GraphVertex> Vertices { get; }
    public IReadOnlyList<GraphArc> Arcs { get; }
    public bool IsDirected { get; }
    public string StartId { get; }
    public string EndId { get; }
    public bool UsesWeights { get; }

    public Dictionary<string, decimal> Distances { get; } = new();
    public Dictionary<string, string?> Previous { get; } = new();
    public Dictionary<string, string?> PreviousArc { get; } = new();
    public HashSet<string> Visited { get; } = new();
    public HashSet<string> Frontier { get; } = new();
    public List<SearchStep> Steps { get; } = new();

    /// <summary>Если алгоритм восстанавливает путь сам (Флойд — Уоршелл, двунаправленный поиск).</summary>
    public List<string>? ExplicitPathNodes { get; set; }
    public List<string>? ExplicitPathArcs { get; set; }

    /// <summary>Сообщение вместо «Путь не найден», например об отрицательном цикле.</summary>
    public string? FailureReason { get; set; }

    // ---------- граф ----------

    /// <summary>Рёбра, по которым можно уйти из вершины (с учётом ориентированности).</summary>
    public IReadOnlyList<(GraphArc Arc, string To)> Outgoing(string nodeId) => _out[nodeId];

    /// <summary>Рёбра, по которым можно прийти в вершину — для обратного поиска.</summary>
    public IReadOnlyList<(GraphArc Arc, string From)> Incoming(string nodeId) => _in[nodeId];

    /// <summary>Длина ребра для алгоритма: вес или 1, если алгоритм веса не учитывает.</summary>
    public decimal Length(GraphArc arc) => UsesWeights ? arc.Weight ?? 1m : 1m;

    /// <summary>Настоящий вес ребра — для подсчёта стоимости найденного пути.</summary>
    public static decimal RealWeight(GraphArc arc) => arc.Weight ?? 1m;

    public string Label(string nodeId) => _labels.GetValueOrDefault(nodeId, nodeId);

    public GraphVertex Vertex(string nodeId) => Vertices.First(v => v.Id == nodeId);

    public static string Format(decimal value) =>
        value == decimal.MaxValue ? "∞" : value.ToString("0.##", CultureInfo.CurrentCulture);

    // ---------- шаги ----------

    public void Record(string title, string description, string? currentNodeId = null, string? examinedArcId = null)
    {
        // Каждый шаг хранит копии коллекций — это «снимок» состояния на момент шага
        Steps.Add(new SearchStep
        {
            Index = Steps.Count,
            Title = title,
            Description = description,
            CurrentNodeId = currentNodeId,
            ExaminedEdgeId = examinedArcId,
            VisitedNodeIds = new HashSet<string>(Visited),
            FrontierNodeIds = new HashSet<string>(Frontier),
            Distances = new Dictionary<string, decimal>(Distances),
            Previous = new Dictionary<string, string?>(Previous)
        });
    }

    /// <summary>Запоминает, что в вершину <paramref name="to"/> пришли из <paramref name="from"/> по ребру.</summary>
    public void SetParent(string to, string from, GraphArc arc)
    {
        Previous[to] = from;
        PreviousArc[to] = arc.Id;
    }

    /// <summary>Сбрасывает метки (для алгоритмов, которые запускают поиск несколько раз).</summary>
    public void ResetMarks()
    {
        foreach (var v in Vertices)
        {
            Distances[v.Id] = decimal.MaxValue;
            Previous[v.Id] = null;
            PreviousArc[v.Id] = null;
        }

        Visited.Clear();
        Frontier.Clear();
    }

    /// <summary>Добавляет завершающий шаг: путь восстанавливается по дереву предков (или берётся готовый).</summary>
    public void Finish(string title)
    {
        var (nodes, arcs) = ExplicitPathNodes is not null
            ? (ExplicitPathNodes, ExplicitPathArcs ?? new List<string>())
            : ReconstructPath();

        var arcById = Arcs.ToDictionary(a => a.Id);
        var found = nodes.Count > 0;
        var cost = arcs.Sum(id => RealWeight(arcById[id]));

        Frontier.Clear();
        Steps.Add(new SearchStep
        {
            Index = Steps.Count,
            Title = title,
            Description = found
                ? $"Путь {string.Join(" → ", nodes.Select(Label))}: рёбер — {arcs.Count}, суммарный вес — {Format(cost)}"
                : FailureReason ?? "Путь не найден",
            CurrentNodeId = EndId,
            VisitedNodeIds = new HashSet<string>(Visited),
            Distances = new Dictionary<string, decimal>(Distances),
            Previous = new Dictionary<string, string?>(Previous),
            FinalPathNodeIds = nodes,
            FinalPathEdgeIds = arcs,
            IsFinished = true,
            PathCost = found ? cost : null
        });
    }

    private (List<string> Nodes, List<string> Arcs) ReconstructPath()
    {
        if (StartId != EndId && Previous[EndId] is null)
            return (new List<string>(), new List<string>());

        var nodes = new List<string>();
        var arcs = new List<string>();
        string? current = EndId;
        var guard = 0;

        while (current is not null && guard++ <= Vertices.Count)
        {
            nodes.Add(current);
            if (current == StartId)
                break;

            if (PreviousArc[current] is { } arcId)
                arcs.Add(arcId);
            current = Previous[current];
        }

        nodes.Reverse();
        arcs.Reverse();

        return nodes.Count > 0 && nodes[0] == StartId
            ? (nodes, arcs)
            : (new List<string>(), new List<string>());
    }

    /// <summary>Самое лёгкое ребро между двумя соседними вершинами пути (для восстановления по матрице).</summary>
    public GraphArc? BestArc(string from, string to) =>
        Outgoing(from).Where(o => o.To == to).Select(o => o.Arc).OrderBy(Length).FirstOrDefault();
}
