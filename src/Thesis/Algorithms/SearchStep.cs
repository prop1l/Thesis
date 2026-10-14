using System.Collections.Generic;

namespace Thesis.Algorithms;

/// <summary>
/// Снимок состояния алгоритма поиска пути на одном шаге — по нему редактор подсвечивает граф.
/// </summary>
public class SearchStep
{
    public int Index { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    /// <summary>Вершина, с которой алгоритм работает на этом шаге.</summary>
    public string? CurrentNodeId { get; set; }

    /// <summary>Ребро, которое проверяется на этом шаге.</summary>
    public string? ExaminedEdgeId { get; set; }

    /// <summary>Уже обработанные вершины.</summary>
    public HashSet<string> VisitedNodeIds { get; set; } = new();

    /// <summary>Вершины, ожидающие обработки: очередь, стек или открытое множество — зависит от алгоритма.</summary>
    public HashSet<string> FrontierNodeIds { get; set; } = new();

    /// <summary>Метки вершин: расстояние, число рёбер или глубина (что именно — см. DistanceCaption алгоритма).</summary>
    public Dictionary<string, decimal> Distances { get; set; } = new();
    public Dictionary<string, string?> Previous { get; set; } = new();

    public List<string> FinalPathNodeIds { get; set; } = new();
    public List<string> FinalPathEdgeIds { get; set; } = new();

    public bool IsFinished { get; set; }

    /// <summary>Суммарный вес найденного пути (только на последнем шаге).</summary>
    public decimal? PathCost { get; set; }
}
