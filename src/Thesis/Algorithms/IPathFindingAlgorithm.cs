using System.Collections.Generic;

namespace Thesis.Algorithms;

/// <summary>
/// Алгоритм поиска пути с пошаговой визуализацией. Все алгоритмы получают одинаковый
/// «снимок» графа и возвращают последовательность шагов одного формата, поэтому редактор
/// показывает любой из них одинаково.
/// </summary>
public interface IPathFindingAlgorithm
{
    string Name { get; }

    /// <summary>Короткое описание идеи алгоритма для панели редактора.</summary>
    string Description { get; }

    /// <summary>Временная сложность, например «O((V + E) log V)».</summary>
    string Complexity { get; }

    /// <summary>Что означают числа под вершинами: расстояние, число рёбер, глубина.</summary>
    string DistanceCaption { get; }

    /// <summary>Учитывает ли алгоритм веса рёбер (иначе каждое ребро считается за 1).</summary>
    bool UsesWeights { get; }

    /// <summary>Гарантирует ли кратчайший путь (по весам или по числу рёбер — см. UsesWeights).</summary>
    bool GuaranteesShortestPath { get; }

    /// <summary>
    /// Выполняет поиск и возвращает все шаги. Последний шаг всегда помечен IsFinished
    /// и содержит найденный путь (или пустой, если пути нет).
    /// </summary>
    IReadOnlyList<SearchStep> BuildSteps(
        IReadOnlyList<GraphVertex> vertices,
        IReadOnlyList<GraphArc> arcs,
        bool isDirected,
        string startId,
        string endId);
}
